resource_group="mynabster"
function_app="mynabster-functionapp"

cd ../Nabster.Functions
dotnet clean --configuration Release /property:GenerateFullPaths=true /consoleloggerparameters:NoSummary
dotnet publish --configuration Release /property:GenerateFullPaths=true /consoleloggerparameters:NoSummary
cd bin/Release/net8.0/publish/
zip functions.zip . -r
az functionapp deployment source config-zip -g $resource_group -n $function_app --src functions.zip 
rm functions.zip
cd ../../../../../Deployments
