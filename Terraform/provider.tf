terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "=2.79.0"
    }
  }
  backend "azurerm" {
    access_key           = var.ACCESS_KEY
    container_name       = "terraform"
    key                  = "terraform.tfstate"
    storage_account_name = "showcasedeploy"
  }
}

provider "azurerm" {
  features {}
  subscription_id = var.SUBSCRIPTION_ID
  tenant_id       = var.TENANT_ID
}
