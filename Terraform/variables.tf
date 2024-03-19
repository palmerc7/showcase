variable "SUBSCRIPTION_ID" {
  default = ""
}

variable "TENANT_ID" {
  default = ""
}

variable "analytics_tags" {
  description = "The default tags for Prod cloud resources"
  type        = map(any)
  default = {
    "env"     = "Production"
    "purpose" = "Analytics"
    "region"  = "Central US"
  }
}

variable "analytics_tags_stage" {
  description = "The default tags for Stage cloud resources"
  type        = map(any)
  default = {
    "env"     = "Staging"
    "purpose" = "Analytics"
    "region"  = "Central US"
  }
}

variable "app_adminui_build_number" {
  default = "latest"
}

variable "app_adminui_build_number_stage" {
  default = "latest"
}

variable "app_adminui_command_line" {
  default = ""
}

variable "app_adminui_container_name" {
  default = "showcasebeta-adminui"
}

variable "app_adminui_container_name_stage" {
  default = "app-stage-showcase-adminui"
}

variable "app_applyui_build_number" {
  default = "latest"
}

variable "app_applyui_command_line" {
  default = ""
}

variable "app_applyui_container_name" {
  default = "showcasebeta-ui"
}

variable "app_api_build_number" {
  default = "latest"
}

variable "app_api_build_number_stage" {
  default = "latest"
}

variable "app_adminservices_ms_build_number_stage" {
  default = "latest"
}

variable "app_api_command_line" {
  default = "-p 80:80"
}

variable "app_api_container_name" {
  default = "showcasebeta-api"
}

variable "app_api_container_name_stage" {
  default = "app-stage-showcase-api"
}

variable "app_adminservices_ms_container_name" {
  default = "showcasebeta-adminservices"
}

variable "app_adminservices_ms_container_name_stage" {
  default = "showcasestage-adminservices"
}

variable "app_identity_build_number" {
  default = "latest"
}

variable "app_identity_build_number_stage" {
  default = "latest"
}

variable "app_identity_command_line" {
  default = "-p 80:80"
}

variable "app_identity_container_name" {
  default = "showcasebeta-identity"
}

variable "app_identity_container_name_stage" {
  default = "app-stage-showcase-identity"
}

variable "app_ui_build_number" {
  default = "latest"
}

variable "app_ui_build_number_stage" {
  default = "latest"
}

variable "app_ui_command_line" {
  default = ""
}

variable "app_ui_container_name" {
  default = "showcasebeta-ui"
}

variable "app_ui_container_name_stage" {
  default = "app-stage-showcase-ui"
}

variable "app_tags" {
  description = "The default tags for cloud resources"
  type        = map(any)
  default = {
    "env"     = "Production"
    "purpose" = "App Tier"
    "region"  = "Central US"
  }
}

variable "app_tags_dr" {
  description = "The default tags for D/R cloud resources"
  type        = map(any)
  default = {
    "env"     = "Disaster Recovery"
    "purpose" = "App Tier"
    "region"  = "East US"
  }
}

variable "app_tags_security" {
  description = "The default tags for cloud resources"
  type        = map(any)
  default = {
    "env"     = "Production"
    "purpose" = "Security"
    "region"  = "Central US"
  }
}

variable "app_tags_stage" {
  description = "The default tags for Stage cloud resources"
  type        = map(any)
  default = {
    "env"     = "Staging"
    "purpose" = "App Tier"
    "region"  = "Central US"
  }
}

variable "container_registry_passwd" {
  description = "The Container Registry Password to set"
  sensitive   = true
  type        = string
}

variable "container_registry_url" {
  description = "The Container Registry Base URL to set"
}

variable "container_registry_userid" {
  description = "The Container Registry Admin UserID to set"
  sensitive   = true
  type        = string
}

variable "database_tags" {
  description = "The default tags for database resources"
  type        = map(any)
  default = {
    "env"     = "Production"
    "purpose" = "Database"
    "region"  = "Central US"
  }
}

variable "database_tags_dr" {
  description = "The default tags for D/R database resources"
  type        = map(any)
  default = {
    "env"     = "Disaster Recovery"
    "purpose" = "Database"
    "region"  = "East US"
  }
}

variable "database_tags_stage" {
  description = "The default tags for Stage database resources"
  type        = map(any)
  default = {
    "env"     = "Staging"
    "purpose" = "Database"
    "region"  = "Central US"
  }
}

variable "function_tags_stage" {
  description = "The default tags for Stage serverless functions"
  type        = map(any)
  default = {
    "env"     = "Staging"
    "purpose" = "Serverless"
    "region"  = "Central US"
  }
}

variable "function_tags" {
  description = "The default tags for Production serverless functions"
  type        = map(any)
  default = {
    "env"     = "Production"
    "purpose" = "Serverless"
    "region"  = "Central US"
  }
}


variable "keyvault_connectionstrings_azureblob" {
  sensitive = true
  type      = string
}

variable "keyvault_data_protection_key" {
  sensitive = true
  type      = string
}

variable "keyvault_FileScannerApiEnabled" {
  sensitive = true
  type      = string
}

variable "keyvault_FileScannerService_Url" {
  sensitive = true
  type      = string
}

variable "location" {
  description = "The location/region where Production resources are created"
  default     = "Central US"
}

variable "location_demo" {
  description = "The location/region where Demo resources are created"
  default     = "Central US"
}

variable "location_dev" {
  description = "The location/region where Dev resources are created"
  default     = "Central US"
}

variable "location_dr" {
  description = "The location/region where D/R resources are created"
  default     = "East US"
}

variable "location_shared" {
  description = "The location/region where Shared resources are created"
  default     = "Central US"
}

variable "location_stage" {
  description = "The location/region where Stage resources are created"
  default     = "Central US"
}

variable "location_uat" {
  description = "The location/region where UAT resources are created"
  default     = "Central US"
}

variable "ms_tags_stage" {
  description = "The default tags for Stage cloud microservice resources"
  type        = map(any)
  default = {
    "env"     = "Staging"
    "purpose" = "Containers"
    "region"  = "Central US"
  }
}

variable "ms_tags" {
  description = "The default tags for Production cloud microservice resources"
  type        = map(any)
  default = {
    "env"     = "Production"
    "purpose" = "Containers"
    "region"  = "Central US"
  }
}

variable "security_tags" {
  description = "The default tags for Stage security resources"
  type        = map(any)
  default = {
    "env"     = "Production"
    "purpose" = "Security"
    "region"  = "Central US"
  }
}

variable "security_tags_stage" {
  description = "The default tags for Stage security resources"
  type        = map(any)
  default = {
    "env"     = "Staging"
    "purpose" = "Security"
    "region"  = "Central US"
  }
}

variable "support_keyvault_functions_internal_object_id" {
  description = "The Azure AD Object Id for Internal Functions"
}

variable "support_keyvault_functions_internal_stage_object_id" {
  description = "The Azure AD Object Id for Internal Functions"
}

variable "support_keyvault_frontdoor_object_id" {
  description = "The Azure AD Object Id for FrontDoor"
}

variable "support_keyvault_frontdoor_stage_object_id" {
  description = "The Azure AD Object Id for FrontDoor"
}

variable "support_keyvault_person1_object_id" {
  description = "The Azure AD Object Id for Support: chris"
}

variable "support_keyvault_person2_object_id" {
  description = "The Azure AD Object Id for Support: brent"
}

variable "support_keyvault_person3_object_id" {
  description = "The Azure AD Object Id for Support: rajesh"
}

variable "support_sql_group_login_username" {
  default     = "SQLAdmins"
  description = "The Azure AD Group for SQL Admins"
}

variable "support_sql_group_object_id" {
  description = "The Azure AD Group Object Id for SQL Admins"
}

variable "sql_connection_tcp_port" {
  description = "The ENV specific SQL Connection string for App Services"
  default     = 1433
}

variable "sql_connection_server" {
  description = "The ENV specific SQL Server name"
}

variable "sql_connection_server_stage" {
  description = "The ENV specific SQL Server name"
}

variable "sql_connection_prefix" {
  description = "The ENV specific SQL Connection string for App Services"
  default     = "Server=tcp"
}

variable "sql_connection_suffix" {
  description = "The ENV specific SQL Connection string for App Services"
  default     = "Persist Security Info=False;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=120;"
}

variable "sql_database_name" {
  description = "The ENV specific SQL Database name"
}

variable "sql_database_name_stage" {
  description = "The ENV specific SQL Database name"
}

variable "sql_server_admin" {
  default = "jammil@showcase.com"
}

variable "sql_server_passwd" {
  description = "The SQL Server Admin Password to set"
  sensitive   = true
  type        = string
}

variable "sql_server_passwd_stage" {
  description = "The SQL Server Admin Password to set"
  sensitive   = true
  type        = string
}

variable "sql_server_userid" {
  description = "The SQL Server Admin UserID to create"
  sensitive   = true
  type        = string
}

variable "sql_server_userid_stage" {
  description = "The SQL Server Admin UserID to create"
  sensitive   = true
  type        = string
}

variable "storage_tags" {
  description = "The default tags for storage resources"
  type        = map(any)
  default = {
    "env"     = "Production"
    "purpose" = "Storage"
    "region"  = "Central US"
  }
}

variable "storage_tags_stage" {
  description = "The default tags for storage resources"
  type        = map(any)
  default = {
    "env"     = "Staging"
    "purpose" = "Storage"
    "region"  = "Central US"
  }
}

variable "tags" {
  description = "The default tags for environment resources"
  type        = map(any)
  default = {
    "env"    = "Production"
    "region" = "Central US"
  }
}

variable "tags_demo" {
  description = "The default tags for Demo environment resources"
  type        = map(any)
  default = {
    "env"    = "Demo"
    "region" = "Central US"
  }
}

variable "tags_dev" {
  description = "The default tags for Dev environment resources"
  type        = map(any)
  default = {
    "env"    = "Development"
    "region" = "Central US"
  }
}

variable "tags_dr" {
  description = "The default tags for D/R environment resources"
  type        = map(any)
  default = {
    "env"    = "Disaster Recovery"
    "region" = "East US"
  }
}

variable "tags_shared" {
  description = "The default tags for Shared Dev environment resources"
  type        = map(any)
  default = {
    "env"    = "Shared Development"
    "region" = "Central US"
  }
}

variable "tags_stage" {
  description = "The default tags for Stage environment resources"
  type        = map(any)
  default = {
    "env"    = "Staging"
    "region" = "Central US"
  }
}

variable "tags_uat" {
  description = "The default tags for UAT environment resources"
  type        = map(any)
  default = {
    "env"    = "User Acceptance Testing"
    "region" = "Central US"
  }
}
