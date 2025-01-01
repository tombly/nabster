if [ -z "$1" ]; then
    echo "Usage: deploy-code.sh <resource_name>"
    exit 1
fi

resource_group=$1
function_app="$1-functionapp"

echo "Deploying to $function_app..."

cd ../Nabster.Chat.Functions
dotnet clean --configuration Release /property:GenerateFullPaths=true /consoleloggerparameters:NoSummary
dotnet publish --configuration Release /property:GenerateFullPaths=true /consoleloggerparameters:NoSummary
cd bin/Release/net9.0/publish/
zip functions.zip . -r
az functionapp deployment source config-zip -g $resource_group -n $function_app --src functions.zip 
rm functions.zip
cd ../../../../../Deployments
