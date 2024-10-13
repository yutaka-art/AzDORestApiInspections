param environmentName string = 'zenn-39890c09bdcda2'
param storageAccountName string = 'st${replace(environmentName, '-', '')}'
param logAnalyticsWorkspaceName string = 'logs-${environmentName}'
param appInsightsName string = 'appi-${environmentName}'
param hostingPlanName string = 'plan-${environmentName}'
param functionAppName string = 'func-${environmentName}'
param location string = resourceGroup().location
param managedIdName string = 'id-${environmentName}'

// ManagedIdentity
resource managedId 'Microsoft.ManagedIdentity/userAssignedIdentities@2018-11-30' = {
  name: managedIdName
  location: location
}

// LogAnalyticsWorkspace
resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2020-08-01' = {
  name: logAnalyticsWorkspaceName
  location: location
  properties: any({
    retentionInDays: 30
    features: {
      searchVersion: 1
      legacy: 0
      enableLogAccessUsingOnlyResourcePermissions: true
    }
    sku: {
      name: 'PerGB2018'
    }
  })
}

// Application Insights
resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightsName
  location: location
  kind: 'web'
  properties: { 
    Application_Type: 'web'
    WorkspaceResourceId:logAnalyticsWorkspace.id
  }
}

// Storage account
var strageSku = 'Standard_LRS'

resource storageAccount 'Microsoft.Storage/storageAccounts@2021-04-01' = {
  name: storageAccountName
  location: location
  kind: 'Storage'
  sku:{
    name: strageSku
  }
}

// Function App Plan
resource hostingPlan  'Microsoft.Web/serverfarms@2021-03-01' = {
  name: hostingPlanName
  location: location
  sku: {
    name: 'EP1'
    tier: 'ElasticPremium'
    size: 'EP1'
  }
  properties: {
    reserved: true // for Linux
  }
}

// Function App
resource functionApp 'Microsoft.Web/sites@2021-03-01' = {
  name: functionAppName
  location: location
  kind: 'functionapp,linux'
  properties: {
    serverFarmId: hostingPlan.id
    siteConfig: {
      appSettings: [
        {
          name: 'APPINSIGHTS_INSTRUMENTATIONKEY'
          value: appInsights.properties.InstrumentationKey
        }
        {
          name: 'AzureWebJobsStorage'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccountName};EndpointSuffix=${environment().suffixes.storage};AccountKey=${storageAccount.listKeys().keys[0].value}'
        }
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet'
        }
        {
          name: 'WEBSITE_CONTENTAZUREFILECONNECTIONSTRING'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccountName};EndpointSuffix=${environment().suffixes.storage};AccountKey=${storageAccount.listKeys().keys[0].value}'
        }
        {
          name: 'TenantId'
          value: '*'
        }
        {
          name: 'AzureDevOpsOrg'
          value: '*'
        }
        {
          name: 'ManagedIdentityClientId'
          value: managedId.properties.clientId
        }
      ]
      ftpsState: 'FtpsOnly'
      minTlsVersion: '1.2'
      cors: {
        allowedOrigins: [
          'https://portal.azure.com'
        ]
        supportCredentials: true
      }
    }
    httpsOnly: true
  }
}
