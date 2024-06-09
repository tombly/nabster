
# Create variables for our resource names.
resource_group="mynabster"
function_app="mynabster-functionapp"
key_vault="mynabster-keyvault"
logic_app="mynabster-logicapp"

# Create a resource group and deploy the infrastructure into it.
az group create --name $resource_group --location westus
az deployment group create --name deploy --resource-group $resource_group --template-file infra.bicep --parameters infra.bicepparam

# Grant the function app's and logic app's managed identities the Key Vault
# Secrets User role for the key vault secrets they need.
subscriptionid=$(az account show --query id -o tsv)
scope_prefix="/subscriptions/$subscriptionid/resourcegroups/$resource_group/providers/Microsoft.KeyVault/vaults/$key_vault/secrets"

func_identity_id=$(az functionapp identity show --name $function_app --resource-group $resource_group --query "principalId" -o tsv)
az role assignment create --role 4633458b-17de-408a-b874-0445c86b69e6 --assignee $func_identity_id --scope $scope_prefix/YnabAccessToken
az role assignment create --role 4633458b-17de-408a-b874-0445c86b69e6 --assignee $func_identity_id --scope $scope_prefix/TwilioAccountSid
az role assignment create --role 4633458b-17de-408a-b874-0445c86b69e6 --assignee $func_identity_id --scope $scope_prefix/TwilioAuthToken
az role assignment create --role 4633458b-17de-408a-b874-0445c86b69e6 --assignee $func_identity_id --scope $scope_prefix/TwilioPhoneNumber

logic_identity_id=$(az resource show --name $logic_app --resource-group $resource_group --resource-type "Microsoft.Logic/workflows" --query "identity.principalId" -o tsv)
az role assignment create --role 4633458b-17de-408a-b874-0445c86b69e6 --assignee $logic_identity_id --scope $scope_prefix/FunctionAppKey

# Deploy the function app code.
cd ../Nabster.Functions
dotnet clean --configuration Release /property:GenerateFullPaths=true /consoleloggerparameters:NoSummary
dotnet publish --configuration Release /property:GenerateFullPaths=true /consoleloggerparameters:NoSummary
cd bin/Release/net8.0/publish/
zip functions.zip . -r
az functionapp deployment source config-zip -g $resource_group -n $function_app --src functions.zip 
rm functions.zip
cd ../../../../../Deployments

# Assign yourself the "Key Vault Administrator" role so that the CLI can access
# the key vault when you run it.
az role assignment create --role 00482a5a-887f-4fb3-b363-3b7fe8e74483 --assignee $(az ad signed-in-user show --query id -o tsv) --scope $(az keyvault show --name $key_vault --query id -o tsv)
