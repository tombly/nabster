if [ -z "$1" ] || [ -z "$2" ]; then
    echo "Usage: deploy-infra.sh <resource_name> <parameter_file>"
    exit 1
fi

# Create variables for our resource names.
resource_group=$1
function_app="$1-functionapp"
key_vault="$1-keyvault"
logic_app="$1-logicapp"
openai="$1-openai"

echo "Deploying to $resource_group..."

# Create a resource group and deploy the infrastructure into it.
az group create --name $resource_group --location westus
az deployment group create --name deploy --resource-group $resource_group --template-file infra.bicep --parameters $2

# Grant the function app's and logic app's managed identities the "Key Vault
# Secrets User" role for the key vault secrets they need.
subscriptionid=$(az account show --query id -o tsv)
scope_prefix=$(az keyvault show --name $key_vault --query id -o tsv)/secrets

func_identity_id=$(az functionapp identity show --name $function_app --resource-group $resource_group --query "principalId" -o tsv)
az role assignment create --role 4633458b-17de-408a-b874-0445c86b69e6 --assignee $func_identity_id --scope $scope_prefix/YnabAccessToken
az role assignment create --role 4633458b-17de-408a-b874-0445c86b69e6 --assignee $func_identity_id --scope $scope_prefix/TwilioAccountSid
az role assignment create --role 4633458b-17de-408a-b874-0445c86b69e6 --assignee $func_identity_id --scope $scope_prefix/TwilioAuthToken
az role assignment create --role 4633458b-17de-408a-b874-0445c86b69e6 --assignee $func_identity_id --scope $scope_prefix/TwilioPhoneNumber

logic_identity_id=$(az resource show --name $logic_app --resource-group $resource_group --resource-type "Microsoft.Logic/workflows" --query "identity.principalId" -o tsv)
az role assignment create --role 4633458b-17de-408a-b874-0445c86b69e6 --assignee $logic_identity_id --scope $scope_prefix/FunctionAppKey

# Grant the function app's managed identity the "Cognitive Services OpenAI User"
# so the function can call the model's inference endpoint.
az role assignment create --role 5e0bd9bd-7b93-4f28-af87-19fc36ad61bd --assignee $func_identity_id --scope $(az cognitiveservices account show --name $openai --resource-group $resource_group --query id -o tsv)

# Assign yourself the "Key Vault Administrator" role so that the CLI can access
# the key vault.
az role assignment create --role 00482a5a-887f-4fb3-b363-3b7fe8e74483 --assignee $(az ad signed-in-user show --query id -o tsv) --scope $(az keyvault show --name $key_vault --query id -o tsv)

# Assign yourself the "Cognitive Services OpenAI User" role so that the CLI can
# call the model's inference endpoint.
az role assignment create --role 5e0bd9bd-7b93-4f28-af87-19fc36ad61bd --assignee $(az ad signed-in-user show --query id -o tsv) --scope $(az cognitiveservices account show --name $openai --resource-group $resource_group --query id -o tsv)
