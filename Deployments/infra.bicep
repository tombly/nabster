@description('The location of the resources')
param location string = resourceGroup().location

@description('The resource name of the key vault')
param vaultName string

@description('The resource name of the function app')
param functionAppName string

@description('The resource name of the storage account for the function app')
param storageAccountName string

@description('The resource name of the API connection')
param apicName string

@description('The resource name of the logic app')
param logicAppName string

@description('The resource name of the Open AI service')
param openAIAccountName string

@secure()
param ynabAccessToken string

@secure()
param twilioAccountSid string

@secure()
param twilioAuthToken string

@secure()
param twilioPhoneNumber string

@description('The (optional) budget name to send to the function (used by logic app).')
param budget string = ''

@description('The message to send to the function (used by logic app).')
param message string

@description('The phone number to send to the function (used by logic app).')
param toPhone string

resource openAIAccount 'Microsoft.CognitiveServices/accounts@2024-04-01-preview' = {
  name: openAIAccountName
  location: location
  sku: {
    name: 'S0'
  }
  kind: 'OpenAI'
  properties: {
    publicNetworkAccess: 'Enabled'
    customSubDomainName: openAIAccountName
  }
}

resource gpt4 'Microsoft.CognitiveServices/accounts/deployments@2024-04-01-preview' = {
  parent: openAIAccount
  name: 'gpt-4o-mini'
  sku: {
    name: 'Standard'
    capacity: 8
  }
  properties: {
    model: {
      format: 'OpenAI'
      name: 'gpt-4o-mini'
    }
    versionUpgradeOption: 'OnceNewDefaultVersionAvailable'
    currentCapacity: 8
    raiPolicyName: 'Microsoft.Default'
  }
}

resource functionApp 'Microsoft.Web/sites@2021-03-01' = {
  name: functionAppName
  location: location
  kind: 'functionapp'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: hostingPlan.id
    siteConfig: {
      appSettings: [
        {
          name: 'AzureWebJobsStorage'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccountName};EndpointSuffix=${environment().suffixes.storage};AccountKey=${storageAccount.listKeys().keys[0].value}'
        }
        {
          name: 'WEBSITE_CONTENTAZUREFILECONNECTIONSTRING'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccountName};EndpointSuffix=${environment().suffixes.storage};AccountKey=${storageAccount.listKeys().keys[0].value}'
        }
        {
          name: 'WEBSITE_CONTENTSHARE'
          value: toLower(functionAppName)
        }
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'WEBSITE_NODE_DEFAULT_VERSION'
          value: '~14'
        }
        {
          name: 'APPINSIGHTS_INSTRUMENTATIONKEY'
          value: applicationInsights.properties.InstrumentationKey
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet-isolated'
        }
        {
          name: 'YNAB_ACCESS_TOKEN'
          value: '@Microsoft.KeyVault(VaultName=${vault.name};SecretName=YnabAccessToken)'
        }
        {
          name: 'TWILIO_ACCOUNT_SID'
          value: '@Microsoft.KeyVault(VaultName=${vault.name};SecretName=TwilioAccountSid)'
        }
        {
          name: 'TWILIO_AUTH_TOKEN'
          value: '@Microsoft.KeyVault(VaultName=${vault.name};SecretName=TwilioAuthToken)'
        }
        {
          name: 'TWILIO_PHONE_NUMBER'
          value: '@Microsoft.KeyVault(VaultName=${vault.name};SecretName=TwilioPhoneNumber)'
        }
        {
          name: 'OPENAI_URL'
          value: 'https://${openAIAccountName}.openai.azure.com/'
        }
      ]
      ftpsState: 'FtpsOnly'
      minTlsVersion: '1.2'
      functionAppScaleLimit: 1
    }
    httpsOnly: true
  }
}

resource functionAppHost 'Microsoft.Web/sites/host@2022-09-01' existing = {
  name: 'default'
  parent: functionApp
}

resource storageAccount 'Microsoft.Storage/storageAccounts@2022-05-01' = {
  name: storageAccountName
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'Storage'
  properties: {
    supportsHttpsTrafficOnly: true
    defaultToOAuthAuthentication: true
  }
}

resource applicationInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: functionAppName
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    Request_Source: 'rest'
  }
}

resource vault 'Microsoft.KeyVault/vaults@2021-11-01-preview' = {
  name: vaultName
  location: location
  properties: {
    accessPolicies: []
    enableRbacAuthorization: true
    enableSoftDelete: false
    softDeleteRetentionInDays: 90
    enabledForDeployment: false
    enabledForDiskEncryption: false
    enabledForTemplateDeployment: false
    tenantId: subscription().tenantId
    sku: {
      name: 'standard'
      family: 'A'
    }
    networkAcls: {
      defaultAction: 'Allow'
      bypass: 'AzureServices'
    }
  }
}

resource vaultSecretFunctionAppKey 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: vault
  name: 'FunctionAppKey'
  tags: {
    'file-encoding': 'utf-8'
  }
  properties: {
    attributes: {
      enabled: true
    }
    value: functionAppHost.listKeys().functionKeys.default
  }
}

resource vaultSecretTwilioAccountSid 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: vault
  name: 'TwilioAccountSid'
  tags: {
    'file-encoding': 'utf-8'
  }
  properties: {
    attributes: {
      enabled: true
    }
    value: twilioAccountSid
  }
}

resource vaultSecretTwilioAuthToken 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: vault
  name: 'TwilioAuthToken'
  tags: {
    'file-encoding': 'utf-8'
  }
  properties: {
    attributes: {
      enabled: true
    }
    value: twilioAuthToken
  }
}

resource vaultSecretTwilioPhoneNumber 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: vault
  name: 'TwilioPhoneNumber'
  tags: {
    'file-encoding': 'utf-8'
  }
  properties: {
    attributes: {
      enabled: true
    }
    value: twilioPhoneNumber
  }
}

resource vaultSecretYnabAccessToken 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: vault
  name: 'YnabAccessToken'
  tags: {
    'file-encoding': 'utf-8'
  }
  properties: {
    attributes: {
      enabled: true
    }
    value: ynabAccessToken
  }
}

resource hostingPlan 'Microsoft.Web/serverfarms@2021-03-01' = {
  name: functionAppName
  location: location
  sku: {
    name: 'Y1'
    tier: 'Dynamic'
  }
  properties: {}
}

resource apiConnectionKeyVault 'Microsoft.Web/connections@2016-06-01' = {
  name: apicName
  location: resourceGroup().location
  properties: {
    displayName: 'Key Vault Connector'
    api: {
      id: '/subscriptions/${subscription().subscriptionId}/providers/Microsoft.Web/locations/${resourceGroup().location}/managedApis/keyvault'
    }
    parameterValueType: 'Alternative'
    alternativeParameterValues: {
      vaultName: vaultName
    }
  }
}

resource stg 'Microsoft.Logic/workflows@2019-05-01' = {
  name: logicAppName
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  tags: {
    displayName: logicAppName
  }
  properties: {
    definition: {
      '$schema': 'https://schema.management.azure.com/providers/Microsoft.Logic/schemas/2016-06-01/workflowdefinition.json#'
      contentVersion: '1.0.0.0'
      parameters: {
        '$connections': {
          defaultValue: {}
          type: 'Object'
        }
      }
      triggers: {
        recurrence: {
          recurrence: {
            frequency: 'Day'
            interval: '1'
            schedule: {
              hours: [
                9
                18
              ]
              minutes: [
                0
              ]
            }
            timeZone: 'Pacific Standard Time'
          }
          type: 'Recurrence'
        }
      }
      actions: {
        Get_secret: {
          runAfter: {}
          type: 'ApiConnection'
          inputs: {
            host: {
              connection: {
                name: '@parameters(\'$connections\')[\'keyvault\'][\'connectionId\']'
              }
            }
            method: 'get'
            path: '/secrets/@{encodeURIComponent(\'FunctionAppKey\')}/value'
          }
        }
        HTTP: {
          runAfter: {
            Get_secret: [
              'Succeeded'
            ]
          }
          type: 'Http'
          inputs: {
            uri: 'https://${functionAppName}.azurewebsites.net/api/IncomingMessage'
            method: 'POST'
            headers: {
              'x-functions-key': '@{body(\'Get_secret\')?[\'value\']}'
            }
            body: {
              budget: budget
              body: message
              from: toPhone
            }
          }
          runtimeConfiguration: {
            contentTransfer: {
              transferMode: 'Chunked'
            }
          }
        }
      }
    }
    parameters: {
      '$connections': {
        value: {
          keyvault: {
            id: '/subscriptions/${subscription().subscriptionId}/providers/Microsoft.Web/locations/${location}/managedApis/keyvault'
            connectionId: '/subscriptions/${subscription().subscriptionId}/resourceGroups/${resourceGroup().name}/providers/Microsoft.Web/connections/${apicName}'
            connectionName: apicName
            connectionProperties: {
              authentication: {
                type: 'ManagedServiceIdentity'
              }
            }
          }
        }
      }
    }
  }
}
