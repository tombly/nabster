using './infra.bicep'

param vaultName = 'mynabster-keyvault'
param functionAppName = 'mynabster-functionapp'
param storageAccountName = 'mynabsterfuncstorage'
param apicName = 'mynabster-apic'
param logicAppName = 'mynabster-logicapp'
param ynabAccessToken = '<SET THIS>'
param smtp2GoApiKey = '<SET THIS>'
param smtp2GoEmailAddress = '<SET THIS>'
param toEmailAddresses = ['<SET THIS>']
