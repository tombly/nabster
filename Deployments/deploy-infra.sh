if [ -z "$1" ] || [ -z "$2" ]; then
    echo "Usage: deploy-infra.sh <resource_name> <parameter_file>"
    exit 1
fi

# Create variables for our resource names.
resource_group=$1
function_app="$1-functionapp"
key_vault="$1-keyvault"
logic_app="$1-logicapp"

echo "Deploying to $resource_group..."

# Create a resource group and deploy the infrastructure into it.
az group create --name $resource_group --location westus
az deployment group create --name deploy --resource-group $resource_group --template-file infra.bicep --parameters $2

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

# Assign yourself the "Key Vault Administrator" role so that the CLI can access
# the key vault when you run it.
az role assignment create --role 00482a5a-887f-4fb3-b363-3b7fe8e74483 --assignee $(az ad signed-in-user show --query id -o tsv) --scope $(az keyvault show --name $key_vault --query id -o tsv)
