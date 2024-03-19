# =====================================================================
# showcase_dr Azure Resource Group
# =====================================================================
resource "azurerm_resource_group" "disaster" {
  location = var.location_dr
  name     = "showcase-dr"
  tags     = var.tags_dr
}

# =====================================================================
# showcase_dr Azure Virtual Network
# =====================================================================
resource "azurerm_virtual_network" "disaster" {
  address_space       = ["10.200.0.0/16"]
  location            = var.location_dr
  name                = "vnet-dr-showcase"
  resource_group_name = azurerm_resource_group.disaster.name
  tags                = var.tags_dr
}

# =====================================================================
# showcase_dr Azure Virtual Subnet (Database)
# =====================================================================
resource "azurerm_subnet" "disaster_data" {
  address_prefixes                               = ["10.200.1.0/24"]
  enforce_private_link_endpoint_network_policies = true
  name                                           = "subnet-dr-showcase"
  resource_group_name                            = azurerm_resource_group.disaster.name
  service_endpoints = [
    "Microsoft.Sql",
    "Microsoft.Storage"
  ]
  virtual_network_name                           = azurerm_virtual_network.disaster.name
}

# =====================================================================
# showcase_dr Azure Virtual Subnet (App Tier)
# =====================================================================
resource "azurerm_subnet" "disaster_apps" {
  address_prefixes                               = ["10.200.2.0/24"]
  delegation {
    name = "delegation"
    service_delegation {
      name    = "Microsoft.Web/serverFarms"
      actions = ["Microsoft.Network/virtualNetworks/subnets/action"]
    }
  }
  enforce_private_link_endpoint_network_policies = false
  name                                           = "subnet-apps-dr-showcase"
  resource_group_name                            = azurerm_resource_group.disaster.name
  service_endpoints = [
    "Microsoft.Sql",
    "Microsoft.Storage"
  ]
  virtual_network_name                           = azurerm_virtual_network.disaster.name
}

# =====================================================================
# showcase_dr Azure Virtual Subnet (API)
# =====================================================================
resource "azurerm_subnet" "disaster_api" {
  address_prefixes                               = ["10.200.3.0/24"]
  delegation {
    name = "delegation"
    service_delegation {
      name    = "Microsoft.Web/serverFarms"
      actions = ["Microsoft.Network/virtualNetworks/subnets/action"]
    }
  }
  name                                           = "subnet-api-dr-showcase"
  resource_group_name                            = azurerm_resource_group.disaster.name
  service_endpoints = [
    "Microsoft.Web",
    "Microsoft.Sql",
    "Microsoft.Storage"
  ]
  virtual_network_name                           = azurerm_virtual_network.disaster.name
}

# =====================================================================
# showcase_dr Azure SQL Server
# =====================================================================
resource "azurerm_mssql_server" "disaster" {
  administrator_login           = var.sql_server_userid
  administrator_login_password  = var.sql_server_passwd
  azuread_administrator {
    login_username = var.support_sql_group_login_username
    object_id      = var.support_sql_group_object_id
    tenant_id      = data.azurerm_client_config.current.tenant_id
  }
  identity {
    type = "SystemAssigned"
  }
  name                          = "sampledr"
  location                      = var.location_dr
  minimum_tls_version           = "1.2"
  public_network_access_enabled = true
  resource_group_name           = azurerm_resource_group.disaster.name
  version                       = "12.0"
  tags                          = var.database_tags_dr
}

# =====================================================================
# showcase_dr Azure SQL Server - Vnet Rule(s)
# =====================================================================
resource "azurerm_mssql_virtual_network_rule" "disaster-vnet-rule-1" {
  name                = "sql-vnet-api-rule"
  server_id           = azurerm_mssql_server.disaster.id
  subnet_id           = azurerm_subnet.disaster_api.id
}

resource "azurerm_mssql_virtual_network_rule" "disaster-vnet-rule-2" {
  name                = "sql-vnet-rule"
  server_id           = azurerm_mssql_server.disaster.id
  subnet_id           = azurerm_subnet.disaster_data.id
}

resource "azurerm_mssql_virtual_network_rule" "disaster-vnet-rule-3" {
  name                = "sql-vnet-apps-rule"
  server_id           = azurerm_mssql_server.disaster.id
  subnet_id           = azurerm_subnet.disaster_apps.id
}

# =====================================================================
# showcase_dr Azure SQL Server - Support Firewall Rule(s)
# =====================================================================
resource "azurerm_mssql_firewall_rule" "disaster-firewall-rule-1" {
  name                = "Chris_Support_1"
  server_id           = azurerm_mssql_server.disaster.id
  start_ip_address    = "45.17.220.0"
  end_ip_address      = "45.17.220.255"
}

resource "azurerm_mssql_firewall_rule" "disaster-firewall-rule-4" {
  name                = "Microsoft_Backbone"
  server_id           = azurerm_mssql_server.disaster.id
  start_ip_address    = "13.86.103.0"
  end_ip_address      = "13.86.103.255"
}

resource "azurerm_mssql_firewall_rule" "disaster-firewall-rule-5" {
  name                = "Brent_Support_1"
  server_id           = azurerm_mssql_server.disaster.id
  start_ip_address    = "47.185.177.0"
  end_ip_address      = "47.185.177.255"
}

# =====================================================================
# showcase_dr Azure SQL Database Failover Group
# =====================================================================
resource "azurerm_sql_failover_group" "disaster" {
  databases           = [azurerm_mssql_database.production.id]
  name                = "disaster-failover-db"
  resource_group_name = azurerm_resource_group.production.name
  partner_servers {
    id = azurerm_mssql_server.disaster.id
  }
  read_write_endpoint_failover_policy {
    mode          = "Automatic"
    grace_minutes = 60
  }
  server_name         = azurerm_mssql_server.production.name

  tags                             = var.database_tags_dr
}
