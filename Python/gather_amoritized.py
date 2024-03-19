azure_management_api_linked_service = ""
export_date_start_from = ""
export_date_start_to = ""
export_name_amortized = ""
export_type = ""
key_vault_linked_service = ""
key_vault_name = ""
resource_group_name = ""
storage_export_folder = ""
storage_resource_name = ""
synapse_notebook_name = "acquire_azure_billing_amortized"

# debug values
if not azure_management_api_linked_service:
    azure_management_api_linked_service = "azure_management_api"
if not export_date_start_from:
    export_date_start_from = "2023-10-01T00:00:00Z"
if not export_date_start_to:
    export_date_start_to = "2023-10-27T00:00:00Z"
if not export_name_amortized:
    export_name_amortized = "DevAdhocAmortized"
if not export_type:
    export_type = "amortized"
if not key_vault_linked_service:
    key_vault_linked_service = "showcase_key_vault"
if not key_vault_name:
    key_vault_name = "showcase-kv"
if not resource_group_name:
    resource_group_name = "showcase-dev-rg"
if not storage_export_folder:
    storage_export_folder = f"acquired-azure-billing-exports\{export_type}"
if not storage_resource_name:
    storage_resource_name = "showcasedevstorage"


##### Notebook codebase:

#[Docs for Azure Cost Management Python SDK](https://learn.microsoft.com/en-us/python/api/azure-mgmt-costmanagement/azure.mgmt.costmanagement.operations.exportsoperations?view=azure-python)
#[Docs for Azure Resources REST APIs](https://learn.microsoft.com/en-us/rest/api/resources/resources)


from azure.identity import ClientSecretCredential
from azure.mgmt.costmanagement import CostManagementClient
from notebookutils import mssparkutils
import requests

print(f"Starting notebook: {synapse_notebook_name}")

# These environment specific variables will be populated from Key Vault secret retrieval
service_principal_id = ""
service_principal_key = ""
tenant_id = ""

# These environment specific variables will be populated from Azure Mgmt REST API calls
storage_resource_id = None
subscription_id = None

print("Getting Key Vault secrets")
service_principal_client_id = mssparkutils.credentials.getSecret(key_vault_name, "azure-billing-service-principal-id", key_vault_linked_service)
service_principal_client_secret = mssparkutils.credentials.getSecret(key_vault_name, "azure-billing-service-principal-key", key_vault_linked_service)
tenant_id = mssparkutils.credentials.getSecret(key_vault_name, "azure-billing-service-principal-tenant", key_vault_linked_service)

print("Creating a ClientSecretCredential")
secret_credential = ClientSecretCredential(tenant_id, service_principal_client_id, service_principal_client_secret)

print("Creating an Azure CostManagementClient")
cost_management_client = CostManagementClient(secret_credential)

print("Getting an Azure Management REST API token for environment")
azure_base_url = "https://management.azure.com"
secret_credential_token = secret_credential.get_token(f"{azure_base_url}/.default").token

print("Getting the subscription_id for environment")
azure_mgmt_rest_api_url = "https://management.azure.com/subscriptions?api-version=2022-12-01"
subscription_response = requests.get(azure_mgmt_rest_api_url, headers={"Authorization": f"Bearer {secret_credential_token}"})
subscription_response.raise_for_status()
subscription_info = subscription_response.json()

subscription_id = subscription_info['value'][0]['id']
if subscription_id is None:
    mssparkutils.session.stop()
    mssparkutils.notebook.exit({"return_code": 1, "message": "FAILED to get subscription_id"})
print(f"   subscription_id: {subscription_id}")

print("Getting the storage_resource_id for Cost Export targets")
azure_mgmt_rest_api_storage_url = f"https://management.azure.com/{subscription_id}/resourcegroups/{resource_group_name}/providers/Microsoft.Storage/storageAccounts/{storage_resource_name}?api-version=2021-04-01"

storage_resource_response = requests.get(azure_mgmt_rest_api_storage_url, headers={"Authorization": f"Bearer {secret_credential_token}"})
storage_resource_response.raise_for_status()
storage_resource_info = storage_resource_response.json()

storage_resource_id = storage_resource_info['id']
if storage_resource_id is None:
    mssparkutils.session.stop()
    mssparkutils.notebook.exit({"return_code": 1, "message": "FAILED to get storage_resource_id"})
print(f"   storage_resource_id: {storage_resource_id}")

# Define the export scope to the tenant, subscription, resource group, etc.
scope=f'{subscription_id}'

print("Defining the Azure cost export parameters")
parameters = {
    "definition": {
        "type": "AmortizedCost",
        "timeframe": "Custom",
        "time_period": {
            "from_property": f'{export_date_start_from}',
            "to": f'{export_date_start_to}'
        }
    },
    "format": "Csv",
    "deliveryInfo": {
        "destination": {
            "container": f'{storage_resource_name}',
            "resourceId": f'{storage_resource_id}',
            "rootFolderPath": f'{storage_export_folder}',
        },
    }
}

print("Creating / Updating the scoped export")
export = cost_management_client.exports.create_or_update(scope, export_name_amortized, parameters)

print("Executing the export")
cost_management_client.exports.execute(scope, export_name_amortized)

print(f"END notebook: {synapse_notebook_name}")
mssparkutils.notebook.exit({"return_code": 0, "message": "SUCCESS"})
