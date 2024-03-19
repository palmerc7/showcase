# =====================================================================
# showcase Azure Resource Group
# =====================================================================
resource "azurerm_resource_group" "production" {
  location = var.location
  name     = "showcase"
  tags     = var.tags
}

locals {
  ip_range_start_microsoft_backbone = "13.86.103.0"
  ip_range_end_microsoft_backbone   = "13.86.103.255"
  subnet_data_cidr_block            = "10.100.1.0/24"
  subnet_apps_cidr_block            = "10.100.2.0/24"
  subnet_api_cidr_block             = "10.100.3.0/24"
  subnet_apps_windows_cidr_block    = "10.100.4.0/24"
  subnet_microservices_cidr_block   = "10.100.5.0/24"
  subnet_vms_cidr_block             = "10.100.6.0/24"
  vnet_cidr_block                   = "10.100.0.0/16"
}

data "azurerm_client_config" "current" {}

# =====================================================================
# showcase Azure Key Vault
# =====================================================================
resource "azurerm_key_vault" "production" {
  enabled_for_deployment          = true
  enabled_for_disk_encryption     = true
  enabled_for_template_deployment = true
  location                        = var.location
  name                            = "keyvault-showcase"
  purge_protection_enabled        = true
  resource_group_name             = azurerm_resource_group.production.name
  sku_name                        = "standard"
  soft_delete_retention_days      = 7
  tenant_id                       = data.azurerm_client_config.current.tenant_id
  tags                            = var.tags
}

# =====================================================================
# showcase Azure Key Vault - Policy
# =====================================================================
resource "azurerm_key_vault_access_policy" "production_keyvault_policy" {
  key_vault_id = azurerm_key_vault.production.id
  tenant_id    = data.azurerm_client_config.current.tenant_id
  object_id    = var.support_keyvault_person1_object_id

  certificate_permissions = [
    "Get",
    "List",
    "Update",
    "Create",
    "Import",
    "Delete",
    "Backup",
    "Restore",
    "Recover",
    "DeleteIssuers",
    "SetIssuers",
    "ListIssuers",
    "GetIssuers",
    "ManageIssuers",
    "ManageContacts",
    "Purge"
  ]

  key_permissions = [
    "Get",
    "List",
    "Update",
    "Create",
    "Delete",
    "Import",
    "Recover",
    "Backup",
    "Restore",
    "Decrypt",
    "Encrypt",
    "UnwrapKey",
    "WrapKey",
    "Verify",
    "Sign",
    "Purge"
  ]

  secret_permissions = [
    "Get",
    "List",
    "Set",
    "Delete",
    "Recover",
    "Backup",
    "Restore",
    "Purge"
  ]
}

# =====================================================================
# showcase Azure Key Vault - Policy (Internal Functions)
# =====================================================================
resource "azurerm_key_vault_access_policy" "internal_functions" {
  key_vault_id = azurerm_key_vault.production.id
  tenant_id    = data.azurerm_client_config.current.tenant_id
  object_id    = var.support_keyvault_functions_internal_object_id

  secret_permissions = [
    "Get",
    "List"
  ]
}

# =====================================================================
# showcase Azure Key Vault - Policy Front Door CDN
# =====================================================================
resource "azurerm_key_vault_access_policy" "production-frontdoorcdn" {
  key_vault_id = azurerm_key_vault.production.id
  tenant_id    = data.azurerm_client_config.current.tenant_id
  object_id    = var.support_keyvault_frontdoor_object_id

  certificate_permissions = [
    "Get",
    "List",
    "Update",
    "Create",
    "Import",
    "Delete",
    "Backup",
    "Restore",
    "Recover",
    "DeleteIssuers",
    "SetIssuers",
    "ListIssuers",
    "GetIssuers",
    "ManageIssuers",
    "ManageContacts",
    "Purge"
  ]

  key_permissions = [
    "Get",
    "List",
    "Update",
    "Create",
    "Delete",
    "Import",
    "Recover",
    "Backup",
    "Restore",
    "Decrypt",
    "Encrypt",
    "UnwrapKey",
    "WrapKey",
    "Verify",
    "Sign",
    "Purge"
  ]

  secret_permissions = [
    "Get",
    "List",
    "Set",
    "Delete",
    "Recover",
    "Backup",
    "Restore",
    "Purge"
  ]
}

# =====================================================================
# showcase Azure Key Vault - Secret(s)
# =====================================================================
resource "azurerm_key_vault_secret" "container_userid" {
  name         = "container-userid"
  value        = var.container_registry_userid
  key_vault_id = azurerm_key_vault.production.id
  depends_on   = [azurerm_key_vault.production]
}

resource "azurerm_key_vault_secret" "container_passwd" {
  name         = "container-passwd"
  value        = var.container_registry_passwd
  key_vault_id = azurerm_key_vault.production.id
  depends_on   = [azurerm_key_vault.production]
}

resource "azurerm_key_vault_secret" "azureblob_connection" {
  name         = "ConnectionStrings-AzureBlobStorage"
  value        = var.keyvault_connectionstrings_azureblob
  key_vault_id = azurerm_key_vault.production.id
  depends_on   = [azurerm_key_vault.production]
}

resource "azurerm_key_vault_secret" "filescanner_api_enabled" {
  name         = "FileScannerSettings-FileScannerApiEnabled"
  value        = var.keyvault_FileScannerApiEnabled
  key_vault_id = azurerm_key_vault.production.id
  depends_on   = [azurerm_key_vault.production]
}

resource "azurerm_key_vault_secret" "filescanner_service_url" {
  name         = "FileScannerSettings-ServiceUrl"
  value        = var.keyvault_FileScannerService_Url
  key_vault_id = azurerm_key_vault.production.id
  depends_on   = [azurerm_key_vault.production]
}

resource "azurerm_key_vault_secret" "data_protection_key" {
  name         = "DataProtectionSettings-DataProtectionKey"
  value        = var.keyvault_data_protection_key
  key_vault_id = azurerm_key_vault.production.id
  depends_on   = [azurerm_key_vault.production]
}

# =====================================================================
# showcase Azure DDoS Protection Plan
# =====================================================================
resource "azurerm_network_ddos_protection_plan" "production" {
  location            = var.location
  name                = "ddosprotection"
  resource_group_name = azurerm_resource_group.production.name
  tags                = var.tags
}

# =====================================================================
# showcase Azure Virtual Network
# =====================================================================
resource "azurerm_virtual_network" "production" {
  address_space = [local.vnet_cidr_block]
  ddos_protection_plan {
    id     = azurerm_network_ddos_protection_plan.production.id
    enable = false
  }
  location            = var.location
  name                = "vnet-showcase"
  resource_group_name = azurerm_resource_group.production.name
  tags                = var.tags
}

# =====================================================================
# showcase Azure Virtual Subnet (for Data)
# =====================================================================
resource "azurerm_subnet" "production" {
  address_prefixes                               = [local.subnet_data_cidr_block]
  enforce_private_link_endpoint_network_policies = true
  enforce_private_link_service_network_policies  = true
  name                                           = "subnet-showcase"
  resource_group_name                            = azurerm_resource_group.production.name
  service_endpoints = [
    "Microsoft.Sql",
    "Microsoft.Storage"
  ]
  virtual_network_name = azurerm_virtual_network.production.name
}

# =====================================================================
# showcase Azure Network Security Group (for Data Subnet)
# =====================================================================
resource "azurerm_network_security_group" "production" {
  location            = var.location
  name                = "nsg-data-showcase"
  resource_group_name = azurerm_resource_group.production.name

  tags = var.security_tags
}

resource "azurerm_subnet_network_security_group_association" "production" {
  network_security_group_id = azurerm_network_security_group.production.id
  subnet_id                 = azurerm_subnet.production.id
}

# =====================================================================
# showcase Azure Virtual Subnet (for App Tier)
# =====================================================================
resource "azurerm_subnet" "production_apps" {
  address_prefixes = [local.subnet_apps_cidr_block]
  delegation {
    name = "delegation"
    service_delegation {
      name    = "Microsoft.Web/serverFarms"
      actions = ["Microsoft.Network/virtualNetworks/subnets/action"]
    }
  }
  enforce_private_link_endpoint_network_policies = true
  enforce_private_link_service_network_policies  = true
  name                                           = "subnet-apps-showcase"
  resource_group_name                            = azurerm_resource_group.production.name
  service_endpoints = [
    "Microsoft.Web"
  ]
  virtual_network_name = azurerm_virtual_network.production.name
}

# =====================================================================
# showcase Azure Network Security Group (for App Subnet)
# =====================================================================
resource "azurerm_network_security_group" "production_apps" {
  location            = var.location
  name                = "nsg-apps-showcase"
  resource_group_name = azurerm_resource_group.production.name

  tags = var.security_tags
}

resource "azurerm_subnet_network_security_group_association" "production_apps" {
  network_security_group_id = azurerm_network_security_group.production_apps.id
  subnet_id                 = azurerm_subnet.production_apps.id
}

# =====================================================================
# showcase Azure Virtual Subnet (for API)
# =====================================================================
resource "azurerm_subnet" "production_api" {
  address_prefixes = [local.subnet_api_cidr_block]
  delegation {
    name = "delegation"
    service_delegation {
      name    = "Microsoft.Web/serverFarms"
      actions = ["Microsoft.Network/virtualNetworks/subnets/action"]
    }
  }
  enforce_private_link_endpoint_network_policies = true
  enforce_private_link_service_network_policies  = true
  name                                           = "subnet-api-showcase"
  resource_group_name                            = azurerm_resource_group.production.name
  service_endpoints = [
    "Microsoft.Sql",
    "Microsoft.Storage",
    "Microsoft.Web"
  ]
  virtual_network_name = azurerm_virtual_network.production.name
}

# =====================================================================
# showcase Azure Network Security Group (for API Subnet)
# =====================================================================
resource "azurerm_network_security_group" "production_api" {
  location            = var.location
  name                = "nsg-api-showcase"
  resource_group_name = azurerm_resource_group.production.name

  tags = var.security_tags
}

resource "azurerm_subnet_network_security_group_association" "production_api" {
  network_security_group_id = azurerm_network_security_group.production_api.id
  subnet_id                 = azurerm_subnet.production_api.id
}

# =====================================================================
# showcase Azure Virtual Subnet (for Windows App Tier)
# =====================================================================
resource "azurerm_subnet" "production_apps_windows" {
  address_prefixes = [local.subnet_apps_windows_cidr_block]
  delegation {
    name = "delegation"
    service_delegation {
      name    = "Microsoft.Web/serverFarms"
      actions = ["Microsoft.Network/virtualNetworks/subnets/action"]
    }
  }
  enforce_private_link_endpoint_network_policies = true
  enforce_private_link_service_network_policies  = true
  name                                           = "subnet-apps-windows-showcase"
  resource_group_name                            = azurerm_resource_group.production.name
  service_endpoints = [
    "Microsoft.Sql",
    "Microsoft.Storage"
  ]
  virtual_network_name = azurerm_virtual_network.production.name
}

# =====================================================================
# showcase Azure Network Security Group (for Windows Apps Subnet)
# =====================================================================
resource "azurerm_network_security_group" "production_apps_windows" {
  location            = var.location
  name                = "nsg-apps-windows-showcase"
  resource_group_name = azurerm_resource_group.production.name

  tags = var.security_tags
}

resource "azurerm_subnet_network_security_group_association" "production_apps_windows" {
  network_security_group_id = azurerm_network_security_group.production_apps_windows.id
  subnet_id                 = azurerm_subnet.production_apps_windows.id
}

# =====================================================================
# showcase Azure Virtual Subnet (for Microservices)
# =====================================================================
resource "azurerm_subnet" "production_microservices" {
  address_prefixes = [local.subnet_microservices_cidr_block]
  delegation {
    name = "delegation"
    service_delegation {
      name    = "Microsoft.Web/serverFarms"
      actions = ["Microsoft.Network/virtualNetworks/subnets/action"]
    }
  }
  enforce_private_link_endpoint_network_policies = true
  enforce_private_link_service_network_policies  = true
  name                                           = "subnet-microservices-showcase"
  resource_group_name                            = azurerm_resource_group.production.name
  service_endpoints = [
    "Microsoft.Sql",
    "Microsoft.Storage",
    "Microsoft.Web"
  ]
  virtual_network_name = azurerm_virtual_network.production.name
}

# =====================================================================
# showcase Azure Network Security Group (for Microservices Subnet)
# =====================================================================
resource "azurerm_network_security_group" "production_microservices" {
  location            = var.location
  name                = "nsg-microservices-showcase"
  resource_group_name = azurerm_resource_group.production.name

  tags = var.security_tags
}

resource "azurerm_subnet_network_security_group_association" "production_microservices" {
  network_security_group_id = azurerm_network_security_group.production_microservices.id
  subnet_id                 = azurerm_subnet.production_microservices.id
}

# =====================================================================
# showcase Azure Virtual Subnet (for VMs)
# =====================================================================
resource "azurerm_subnet" "production_vms" {
  address_prefixes                               = [local.subnet_vms_cidr_block]
  enforce_private_link_endpoint_network_policies = true
  enforce_private_link_service_network_policies  = true
  name                                           = "subnet-vms-showcase"
  resource_group_name                            = azurerm_resource_group.production.name
  service_endpoints                              = []
  virtual_network_name                           = azurerm_virtual_network.production.name
}

# =====================================================================
# showcase Azure Network Security Group (for VMs Subnet)
# =====================================================================
resource "azurerm_network_security_group" "production_vms" {
  location            = var.location
  name                = "nsg-microservices-showcase"
  resource_group_name = azurerm_resource_group.production.name

  tags = var.security_tags
}

# =====================================================================
# showcase Azure Public IP for NAT Gateway
# =====================================================================
resource "azurerm_public_ip" "production_natgateway" {
  name                = "natgateway-publicip-showcase"
  location            = var.location
  resource_group_name = azurerm_resource_group.production.name
  allocation_method   = "Static"
  sku                 = "Standard"
}

# =====================================================================
# showcase Azure NAT Gateway
# =====================================================================
resource "azurerm_nat_gateway" "production_natgateway" {
  name                    = "natgateway-showcase"
  location                = var.location
  resource_group_name     = azurerm_resource_group.production.name
  sku_name                = "Standard"
  idle_timeout_in_minutes = 10
  zones                   = ["1"]
}

# =====================================================================
# showcase Azure Associating Public IP to NAT Gateway
# =====================================================================
resource "azurerm_nat_gateway_public_ip_association" "production_natgateway" {
  nat_gateway_id       = azurerm_nat_gateway.production_natgateway.id
  public_ip_address_id = azurerm_public_ip.production_natgateway.id
}

# =====================================================================
# showcase Azure Associating API Subnet to NAT Gateway
# =====================================================================
resource "azurerm_subnet_nat_gateway_association" "production_api_natgateway" {
  subnet_id      = azurerm_subnet.production_api.id
  nat_gateway_id = azurerm_nat_gateway.production_natgateway.id
}

# =====================================================================
# showcase Azure Associating Microservices Subnet to NAT Gateway
# =====================================================================
resource "azurerm_subnet_nat_gateway_association" "production_microservices_natgateway" {
  subnet_id      = azurerm_subnet.production_microservices.id
  nat_gateway_id = azurerm_nat_gateway.production_natgateway.id
}

# =====================================================================
# showcase Azure Associating VMs Subnet to NAT Gateway
# =====================================================================
resource "azurerm_subnet_nat_gateway_association" "production_vms_natgateway" {
  subnet_id      = azurerm_subnet.production_vms.id
  nat_gateway_id = azurerm_nat_gateway.production_natgateway.id
}

# =====================================================================
# showcase Azure Storage Account for SQL Audit and Threat Assessment
# =====================================================================
resource "azurerm_storage_account" "production" {
  access_tier              = "Hot"
  account_replication_type = "LRS"
  account_tier             = "Standard"
  location                 = var.location
  min_tls_version          = "TLS1_2"
  name                     = "sqlauditshowcase"
  resource_group_name      = azurerm_resource_group.production.name
  tags                     = var.tags
}

# =====================================================================
# showcase Azure SQL Server
# =====================================================================
resource "azurerm_mssql_server" "production" {
  administrator_login          = var.sql_server_userid
  administrator_login_password = var.sql_server_passwd
  azuread_administrator {
    login_username = var.support_sql_group_login_username
    object_id      = var.support_sql_group_object_id
    tenant_id      = data.azurerm_client_config.current.tenant_id
  }
  identity {
    type = "SystemAssigned"
  }
  name                          = "showcase"
  location                      = var.location
  minimum_tls_version           = "1.2"
  resource_group_name           = azurerm_resource_group.production.name
  version                       = "12.0"
  tags                          = var.database_tags
}

# =====================================================================
# showcase Azure SQL Server Audit Policy
# =====================================================================
resource "azurerm_mssql_server_extended_auditing_policy" "production" {
  log_monitoring_enabled                  = true
  server_id                               = azurerm_mssql_server.production.id
  storage_endpoint                        = azurerm_storage_account.production.primary_blob_endpoint
  storage_account_access_key              = azurerm_storage_account.production.primary_access_key
  storage_account_access_key_is_secondary = false
  retention_in_days                       = 365
}

# =====================================================================
# showcase Azure SQL Server Security Policy
# =====================================================================
resource "azurerm_mssql_server_security_alert_policy" "production" {
  server_name                = azurerm_mssql_server.production.name
  state                      = "Enabled"
  storage_account_access_key = azurerm_storage_account.production.primary_access_key
  storage_endpoint           = azurerm_storage_account.production.primary_blob_endpoint
  resource_group_name        = azurerm_resource_group.production.name
  retention_days             = 90
}

# =====================================================================
# showcase Azure SQL Server Vulnerability Storage Container
# =====================================================================
resource "azurerm_storage_container" "production_sql_container" {
  name                  = "sqlvulnerability"
  storage_account_name  = azurerm_storage_account.production.name
  container_access_type = "private"
}

# =====================================================================
# showcase Azure SQL Server Vulnerability Assessment
# =====================================================================
resource "azurerm_mssql_server_vulnerability_assessment" "production" {
  server_security_alert_policy_id = azurerm_mssql_server_security_alert_policy.production.id
  storage_container_path          = "${azurerm_storage_account.production.primary_blob_endpoint}${azurerm_storage_container.production_sql_container.name}/"
  storage_account_access_key      = azurerm_storage_account.production.primary_access_key

  recurring_scans {
    enabled                   = true
    email_subscription_admins = false
    emails = [
      "chris@showcase.com"
    ]
  }
}

# =====================================================================
# showcase Azure SQL Server Private Endpoint (SQL)
# =====================================================================
resource "azurerm_private_endpoint" "production" {
  location            = var.location
  name                = "sql-private-endpoint"
  resource_group_name = azurerm_resource_group.production.name
  subnet_id           = azurerm_subnet.production.id
  private_service_connection {
    is_manual_connection           = "false"
    name                           = "sql-private-connection"
    private_connection_resource_id = azurerm_mssql_server.production.id
    subresource_names              = ["sqlServer"]
  }
}

# =====================================================================
# showcase Azure SQL Database
# =====================================================================
resource "azurerm_mssql_database" "production" {
  name      = var.sql_database_name
  server_id = azurerm_mssql_server.production.id
  sku_name  = "S3"
  tags      = var.database_tags
  long_term_retention_policy {
    weekly_retention = "P2W"
  }
}

# =====================================================================
# showcase Azure SQL Server - Vnet Rule(s)
# =====================================================================
resource "azurerm_mssql_virtual_network_rule" "production-vnet-rule-1" {
  name      = "sql-vnet-api-rule"
  server_id = azurerm_mssql_server.production.id
  subnet_id = azurerm_subnet.production_api.id
}

resource "azurerm_mssql_virtual_network_rule" "production-vnet-rule-2" {
  name      = "sql-vnet-rule"
  server_id = azurerm_mssql_server.production.id
  subnet_id = azurerm_subnet.production.id
}

resource "azurerm_mssql_virtual_network_rule" "production-vnet-rule-3" {
  name      = "sql-vnet-apps-rule"
  server_id = azurerm_mssql_server.production.id
  subnet_id = azurerm_subnet.production_apps.id
}

resource "azurerm_mssql_virtual_network_rule" "production_vnet_rule_4" {
  name      = "sql-vnet-microservices-rule"
  server_id = azurerm_mssql_server.production.id
  subnet_id = azurerm_subnet.production_microservices.id
}


# =====================================================================
# showcase Azure SQL Server - Support Firewall Rule(s)
# =====================================================================
resource "azurerm_mssql_firewall_rule" "production-firewall-rule-4" {
  name             = "Microsoft_Backbone"
  server_id        = azurerm_mssql_server.production.id
  start_ip_address = local.ip_range_start_microsoft_backbone
  end_ip_address   = local.ip_range_end_microsoft_backbone
}

resource "azurerm_mssql_firewall_rule" "production-firewall-rule-nat" {
  name             = "Nat_Gateway"
  server_id        = azurerm_mssql_server.production.id
  start_ip_address = azurerm_public_ip.production_natgateway.ip_address
  end_ip_address   = azurerm_public_ip.production_natgateway.ip_address
}

# =====================================================================
# showcase Azure Storage Account for Application layer
# =====================================================================
resource "azurerm_storage_account" "production-app" {
  access_tier              = "Hot"
  account_replication_type = "LRS"
  account_tier             = "Standard"
  location                 = var.location
  min_tls_version          = "TLS1_2"
  name                     = "showcasestorage"
  network_rules {
    default_action = "Allow"
    ip_rules = [
    ]
    virtual_network_subnet_ids = [
      azurerm_subnet.production.id,
      azurerm_subnet.production_apps.id,
      azurerm_subnet.production_apps_windows.id
    ]
  }
  resource_group_name = azurerm_resource_group.production.name
  tags                = var.storage_tags
}

# =====================================================================
# showcase Azure SQL Server Private Endpoint (Storage)
# =====================================================================
resource "azurerm_private_endpoint" "production-storage" {
  location            = var.location
  name                = "storage-private-endpoint"
  resource_group_name = azurerm_resource_group.production.name
  subnet_id           = azurerm_subnet.production.id
  private_service_connection {
    is_manual_connection           = "false"
    name                           = "storage-private-connection"
    private_connection_resource_id = azurerm_storage_account.production-app.id
    subresource_names              = ["blob"]
  }
}

# =====================================================================
# showcase Azure App Service Plan (Linux)
# =====================================================================
resource "azurerm_app_service_plan" "production" {
  kind                = "Linux"
  location            = var.location
  name                = "app-plan-showcase"
  reserved            = true
  resource_group_name = azurerm_resource_group.production.name
  sku {
    tier = "PremiumV2"
    size = "P2v2"
  }
  tags = var.app_tags
}

# =====================================================================
# showcase Azure App Service Plan (Windows)
# =====================================================================
resource "azurerm_app_service_plan" "production_windows" {
  kind                = "Windows"
  location            = var.location
  name                = "app-plan-windows-showcase"
  reserved            = false
  resource_group_name = azurerm_resource_group.production.name
  sku {
    tier = "PremiumV2"
    size = "P1v2"
  }
  tags = var.app_tags
}

# =====================================================================
# showcase Azure App Service Plan (Linux)
# =====================================================================
resource "azurerm_app_service_plan" "production_api" {
  kind                = "Linux"
  location            = var.location
  name                = "app-plan-api-showcase"
  reserved            = true
  resource_group_name = azurerm_resource_group.production.name
  sku {
    tier = "PremiumV2"
    size = "P2v2"
  }
  tags = var.app_tags
}

# =====================================================================
# showcase Azure App Service Plan (Linux Microservices)
# =====================================================================
resource "azurerm_app_service_plan" "production_microservices" {
  kind                = "Linux"
  location            = var.location
  name                = "app-plan-ms-showcase"
  reserved            = true
  resource_group_name = azurerm_resource_group.production.name
  sku {
    tier = "PremiumV2"
    size = "P1v2"
  }
  tags = var.ms_tags
}

# =====================================================================
# showcase Azure App Service Plan (Azure Functions)
# =====================================================================
resource "azurerm_app_service_plan" "production_functions" {
  kind                = "functionapp"
  location            = var.location
  name                = "app-plan-functions-showcase"
  reserved            = false
  resource_group_name = azurerm_resource_group.production.name
  sku {
    tier = "Dynamic"
    size = "Y1"
  }
  tags = var.function_tags
}

# =====================================================================
# showcase Azure App Service Plan (Azure Functions Windows)
# =====================================================================
resource "azurerm_app_service_plan" "production_functions_windows" {
  kind                = "Windows"
  location            = var.location
  name                = "app-plan-functions-windows-showcase"
  per_site_scaling    = false
  reserved            = false
  resource_group_name = azurerm_resource_group.production.name
  sku {
    tier = "PremiumV2"
    size = "P1v2"
  }
  zone_redundant = false
  tags           = var.app_tags
}

# =====================================================================
# showcase Azure App Service (API)
# Deploys from Shared Container Registry
# =====================================================================
resource "azurerm_app_service" "app-showcase-api" {
  app_service_plan_id     = azurerm_app_service_plan.production_api.id
  client_affinity_enabled = false
  https_only              = true
  identity {
    type = "SystemAssigned"
  }
  location            = var.location
  name                = "app-showcase-api"
  resource_group_name = azurerm_resource_group.production.name

  app_settings = {
    "ASPNETCORE_ENVIRONMENT"              = "beta",
    "DOCKER_REGISTRY_SERVER_PASSWORD"     = azurerm_key_vault_secret.container_passwd.value,
    "DOCKER_REGISTRY_SERVER_URL"          = "https://${var.container_registry_url}",
    "DOCKER_REGISTRY_SERVER_USERNAME"     = azurerm_key_vault_secret.container_userid.value,
    "WEBSITES_ENABLE_APP_SERVICE_STORAGE" = "false"
  }

  site_config {
    always_on        = true
    app_command_line = var.app_api_command_line
    cors {
      allowed_origins = [
        "https://app.showcase.com",
        "https://app-showcase-ui.azurewebsites.net",
        "https://endpoint-app-showcase.z01.azurefd.net"
      ]
    }
    default_documents        = []
    dotnet_framework_version = "v6.0"
    ftps_state               = "Disabled"
    http2_enabled            = false
    min_tls_version             = "1.2"
    number_of_workers           = 1
    scm_use_main_ip_restriction = false
    use_32_bit_worker_process   = true
    websockets_enabled          = false
    windows_fx_version          = ""
  }

  lifecycle {
    ignore_changes = [
      site_config["linux_fx_version"]
    ]
  }

  tags = var.app_tags
}

# =====================================================================
# showcase Azure App Service VNET Connection (API)
# =====================================================================
resource "azurerm_app_service_virtual_network_swift_connection" "vnet-connect-showcase-api" {
  app_service_id = azurerm_app_service.app-showcase-api.id
  subnet_id      = azurerm_subnet.production_api.id
}

# =====================================================================
# showcase Azure App Service (UI)
# Deploys from Shared Container Registry
# =====================================================================
resource "azurerm_app_service" "app-showcase-ui" {
  app_service_plan_id     = azurerm_app_service_plan.production.id
  client_affinity_enabled = false
  client_cert_enabled     = false
  https_only              = true
  location                = var.location
  name                    = "app-showcase-ui"
  resource_group_name     = azurerm_resource_group.production.name

  app_settings = {
    "DOCKER_REGISTRY_SERVER_PASSWORD"     = azurerm_key_vault_secret.container_passwd.value,
    "DOCKER_REGISTRY_SERVER_URL"          = "https://${var.container_registry_url}",
    "DOCKER_REGISTRY_SERVER_USERNAME"     = azurerm_key_vault_secret.container_userid.value,
    "WEBSITES_ENABLE_APP_SERVICE_STORAGE" = "false"
  }

  site_config {
    always_on                = true
    app_command_line         = var.app_ui_command_line
    default_documents        = []
    dotnet_framework_version = "v4.0"
    ftps_state               = "Disabled"
    http2_enabled            = false
    ip_restriction           = []
    min_tls_version             = "1.2"
    number_of_workers           = 1
    scm_use_main_ip_restriction = false
    use_32_bit_worker_process   = true
    websockets_enabled          = false
    windows_fx_version          = ""
  }

  lifecycle {
    ignore_changes = [
      site_config["linux_fx_version"]
    ]
  }

  tags = var.app_tags
}

# =====================================================================
# showcase Azure App Service VNET Connection (UI)
# =====================================================================
resource "azurerm_app_service_virtual_network_swift_connection" "vnet-connect-showcase-ui" {
  app_service_id = azurerm_app_service.app-showcase-ui.id
  subnet_id      = azurerm_subnet.production_apps.id
}

# =====================================================================
# showcase Azure App Service (AdminServices Microservice)
# Deploys from from Shared Container Registry
# =====================================================================
resource "azurerm_app_service" "app_showcase_ms_adminservices" {
  app_service_plan_id     = azurerm_app_service_plan.production_microservices.id
  client_affinity_enabled = false
  https_only              = true
  identity {
    type = "SystemAssigned"
  }
  location            = var.location
  name                = "app-showcase-ms-adminservices"
  resource_group_name = azurerm_resource_group.production.name

  app_settings = {
    "ASPNETCORE_ENVIRONMENT"              = "beta",
    "DOCKER_REGISTRY_SERVER_PASSWORD"     = azurerm_key_vault_secret.container_passwd.value,
    "DOCKER_REGISTRY_SERVER_URL"          = "https://${var.container_registry_url}",
    "DOCKER_REGISTRY_SERVER_USERNAME"     = azurerm_key_vault_secret.container_userid.value,
    "WEBSITES_ENABLE_APP_SERVICE_STORAGE" = "false"
  }

  site_config {
    always_on = true
    cors {
      allowed_origins = [
        "https://admin.showcase.com",
        "https://app.showcase.com",
        "https://app-showcase-adminui.azurewebsites.net",
        "https://endpoint-admin.z01.azurefd.net"
      ]
    }
    default_documents           = []
    dotnet_framework_version    = "v6.0"
    ftps_state                  = "Disabled"
    http2_enabled               = false
    linux_fx_version            = "DOCKER|${var.container_registry_url}/${var.app_adminservices_ms_container_name}:2569"
    min_tls_version             = "1.2"
    number_of_workers           = 1
    scm_use_main_ip_restriction = false
    use_32_bit_worker_process   = true
    websockets_enabled          = false
    windows_fx_version          = ""
    vnet_route_all_enabled      = true
  }

  lifecycle {
    ignore_changes = [
      site_config["linux_fx_version"]
    ]
  }

  tags = var.ms_tags
}

# ==============================================================================
# showcase Azure App Service VNET Connection (AdminServices Microservice)
# ==============================================================================
resource "azurerm_app_service_virtual_network_swift_connection" "vnet_connect_showcase_ms_adminservices" {
  app_service_id = azurerm_app_service.app_showcase_ms_adminservices.id
  subnet_id      = azurerm_subnet.production_microservices.id
}

# =====================================================================
# showcase Azure Application Insights (Web)
# =====================================================================
resource "azurerm_application_insights" "production-web-insights" {
  application_type    = "web"
  location            = var.location
  name                = "showcase-web-appinsights"
  resource_group_name = azurerm_resource_group.production.name
  retention_in_days   = 30

  lifecycle {
    ignore_changes = [
      tags
    ]
  }

}

# =====================================================================
# showcase Azure Function (Internal)
# =====================================================================
resource "azurerm_function_app" "function_internal_showcase" {
  app_service_plan_id = azurerm_app_service_plan.production_functions.id
  https_only          = true
  identity {
    type = "SystemAssigned"
  }
  location            = var.location
  name                = "functions-internal-showcase"
  resource_group_name = azurerm_resource_group.production.name
  site_config {
    ftps_state               = "FtpsOnly"
    ip_restriction           = []
    dotnet_framework_version = "v6.0"
    min_tls_version          = "1.2"
  }
  storage_account_access_key = azurerm_storage_account.production.primary_access_key
  storage_account_name       = azurerm_storage_account.production.name
  tags                       = var.function_tags
  version                    = "~4"

  lifecycle {
    ignore_changes = [
      app_settings
    ]
  }

}

# =====================================================================
# showcase_stage Azure Function (UrlGo)
# =====================================================================
resource "azurerm_function_app" "function_showcase_urlgo" {
  app_service_plan_id = azurerm_app_service_plan.production_functions_windows.id
  app_settings = {
    "FUNCTIONS_WORKER_RUNTIME" = "dotnet",
    "WEBSITE_RUN_FROM_PACKAGE" = "1"
  }
  https_only = true
  identity {
    type = "SystemAssigned"
  }
  location            = var.location
  name                = "urlgo"
  resource_group_name = azurerm_resource_group.production.name
  site_config {
    dotnet_framework_version = "v6.0"
    ftps_state               = "FtpsOnly"
    ip_restriction           = []
    min_tls_version          = "1.2"
  }
  storage_account_access_key = azurerm_storage_account.production.primary_access_key
  storage_account_name       = azurerm_storage_account.production.name
  tags                       = var.function_tags
  version                    = "~4"

  lifecycle {
    ignore_changes = [
      app_settings["APPINSIGHTS_INSTRUMENTATIONKEY"],
      app_settings["APPLICATIONINSIGHTS_CONNECTION_STRING"],
      app_settings["FUNCTIONS_WORKER_RUNTIME"],
      app_settings["WEBSITE_RUN_FROM_PACKAGE"],
      client_cert_mode,
      daily_memory_time_quota
    ]
  }

}
