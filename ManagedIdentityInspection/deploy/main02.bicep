param environmentName string = 'zenn-92c051f79f4b79'
param subscriptionId string = '{your subscription id}'
param location string = resourceGroup().location

// Managed Identity
module managedId 'managedIdentity.bicep' = {
  name: 'managedId'
  params: {
    environmentName: environmentName
    location: location
  }
}

// Logic Apps
module LogicApps 'logicapps.bicep' = {
  name: 'LogicApps'
  params: {
    environmentName: environmentName
    managedIdName: managedId.outputs.outManagedIdName
    subscriptionId: subscriptionId
    location: location
  }
}
