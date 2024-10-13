param environmentName string = 'zenn-92c051f79f4b79'
param managedIdName string = 'id-${environmentName}'
param location string = resourceGroup().location

// ManagedIdentity
resource managedId 'Microsoft.ManagedIdentity/userAssignedIdentities@2018-11-30' = {
  name: managedIdName
  location: location
}

output outManagedIdName string = managedId.name
