
function Read-XmlElementAsHashtable([System.Xml.XmlElement] $Element)
{
	$hashtable = @{}
	if ($Element.Attributes)
	{
		$Element.Attributes |
			ForEach-Object {
				$boolVal = $null
				if ([bool]::TryParse($_.Value, [ref]$boolVal))
				{
					$hashtable[$_.Name] = $boolVal
				}
				else
				{
					$hashtable[$_.Name] = $_.Value
				}
			}
	}

	return $hashtable
}

function Deploy-Application($Path, $Project)
{
	$DeployScript = (Join-Path $Path "Scripts/Deploy-FabricApplication.ps1")
	$ApplicationPackagePath = (Join-Path $Path "pkg\Release")
	$PublishProfileFile = (Join-Path $Path "PublishProfiles\Cloud.xml")

	# Package application.
	msbuild $Project /t:Package /p:Platform=x64 /p:Configuration=Release

	# Connect to cluster.
	$PublishProfile = [Xml]$(Get-Content $PublishProfileFile)
	$ClusterConnectionParameters = Read-XmlElementAsHashtable $PublishProfile.PublishProfile.Item("ClusterConnectionParameters")
	Connect-ServiceFabricCluster @ClusterConnectionParameters
	$global:clusterConnection = $clusterConnection

	# Deploy application.
	$attempts = 0
	while ($attempts++ -lt 5)
	{
		try
		{
			. $DeployScript `
				-ApplicationPackagePath $ApplicationPackagePath `
				-PublishProfileFile $PublishProfileFile `
				-DeployOnly:$false `
				-ApplicationParameter:@{} `
				-UnregisterUnusedApplicationVersionsAfterUpgrade $false `
				-OverrideUpgradeBehavior 'None' `
				-OverwriteBehavior 'SameAppTypeAndVersion' `
				-UseExistingClusterConnection:$true `
				-SkipPackageValidation:$false `
				-ErrorAction Stop

			break;
		}
		catch
		{
			Write-Warning "Exception deploying: $($_.Exception.Message)"
		}
	}
}
