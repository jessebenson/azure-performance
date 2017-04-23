<#
 .SYNOPSIS
    Deploys a template to Azure

 .DESCRIPTION
    Deploys an Azure Resource Manager template

 .PARAMETER subscriptionId
    The subscription id where the template will be deployed.

 .PARAMETER resourceGroupName
    The resource group where the template will be deployed. Can be the name of an existing or a new resource group.

 .PARAMETER resourceGroupLocation
    A resource group location. If specified, will try to create a new resource group in this location. If not specified, assumes resource group is existing.

 .PARAMETER templateFilePath
    Path to the template file. Defaults to template.json.

 .PARAMETER parametersFilePath
    Path to the parameters file. Defaults to parameters.json. If file is not found, will prompt for parameter values based on template.
#>

param(
    [string] $SubscriptionId = "fc4ea3c9-1d30-4f18-b33b-7404e7da0123",
    [string] $ResourceGroupName = "azure-performance",
    [string] $ResourceGroupLocation = "South Central US",
    [string] $TemplateFilePath = "template.json",
    [string] $ParametersFilePath = "parameters.json"
)

$ErrorActionPreference = "Stop"

# Select subscription
Write-Host "Selecting subscription '$SubscriptionId'";
Select-AzureRmSubscription -SubscriptionID $SubscriptionId;

# Create or check for existing resource group
$resourceGroup = Get-AzureRmResourceGroup -Name $ResourceGroupName -ErrorAction SilentlyContinue
if (!$resourceGroup)
{
    Write-Host "Creating resource group '$ResourceGroupName' in location '$ResourceGroupLocation'";
    New-AzureRmResourceGroup -Name $ResourceGroupName -Location $ResourceGroupLocation
}
else
{
    Write-Host "Using existing resource group '$ResourceGroupName'";
}

# Start the deployment
Write-Host "Starting deployment...";
$deploymentLabel = Get-Date -Format g
if (Test-Path $parametersFilePath)
{
    New-AzureRmResourceGroupDeployment -Name $deploymentLabel -ResourceGroupName $ResourceGroupName -TemplateFile $TemplateFilePath -TemplateParameterFile $ParametersFilePath;
}
else
{
    New-AzureRmResourceGroupDeployment -Name $deploymentLabel -ResourceGroupName $ResourceGroupName -TemplateFile $TemplateFilePath;
}
