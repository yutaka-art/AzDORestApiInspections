param environmentName string = 'zenn-92c051f79f4b79'
param subscriptionId string = '{your subscription id}'
param logicAppsName string = 'logic-${environmentName}'
param managedIdName string = 'id-${environmentName}'
param location string = resourceGroup().location

resource logicApps 'Microsoft.Logic/workflows@2019-05-01' = {
  name: logicAppsName
  location: location
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '/subscriptions/${subscriptionId}/resourceGroups/${resourceGroup().name}/providers/Microsoft.ManagedIdentity/userAssignedIdentities/${managedIdName}': {
      }
    }
  }
  properties: {
    state: 'Enabled'
    definition: {
      '$schema': 'https://schema.management.azure.com/providers/Microsoft.Logic/schemas/2016-06-01/workflowdefinition.json#'
      contentVersion: '1.0.0.0'
      parameters: {
      }
      triggers: {
        request: {
          type: 'Request'
          kind: 'Http'
          inputs: {
            schema: {
              properties: {
                itemid: {
                  type: 'string'
                }
                organization: {
                  type: 'string'
                }
                project: {
                  type: 'string'
                }
              }
              type: 'object'
            }
          }
        }
      }
      actions: {
        'HTTP：Azure_DevOps_Rest_API_Workitem取得': {
          runAfter: {
          }
          type: 'Http'
          inputs: {
            authentication: {
              audience: '499b84ac-1321-427f-aa17-267ca6975798'
              identity: '/subscriptions/${subscriptionId}/resourceGroups/${resourceGroup().name}/providers/Microsoft.ManagedIdentity/userAssignedIdentities/${managedIdName}'
              type: 'ManagedServiceIdentity'
            }
            headers: {
              'Content-Type': 'application/json-patch+json'
            }
            method: 'GET'
            uri: 'https://dev.azure.com/@{triggerBody()?[\'organization\']}/@{triggerBody()?[\'project\']}/_apis/wit/workitems/@{triggerBody()?[\'itemid\']}?api-version=7.1-preview.3'
          }
        }
        Response: {
          runAfter: {
            'HTTP：Azure_DevOps_Rest_API_Workitem取得': [
              'Succeeded'
            ]
          }
          type: 'Response'
          inputs: {
            body: '@body(\'HTTP：Azure_DevOps_Rest_API_Workitem取得\')'
            statusCode: 200
          }
        }
      }
      outputs: {
      }
    }
    parameters: {
    }
  }
}
