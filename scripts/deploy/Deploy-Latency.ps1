Param
(
	[Switch] $All,

	[Switch] $DocumentDB,
	[Switch] $EventHub,
	[Switch] $Redis,
	[Switch] $ServiceFabric,
	[Switch] $SQL,
	[Switch] $Storage,
	[Switch] $Telemetry,
	[Switch] $Web
)

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
}

$LocalFolder = (Split-Path $MyInvocation.MyCommand.Path)
$RootFolder = Split-Path (Split-Path $LocalFolder)
$SrcFolder = Join-Path (Join-Path $RootFolder "src") "Latency"

#
# Build solution.
#

$Solution = (Join-Path $SrcFolder "azure-performance-latency.sln")
msbuild $Solution /t:Clean /p:Platform=x64 /p:Configuration=Release
msbuild $Solution /p:Platform=x64 /p:Configuration=Release

#
# Package and deploy projects.
#

if ($DocumentDB -or $All)
{
	$DocumentDbPath = (Join-Path $SrcFolder "DocumentDB/Latency.DocumentDB")
	$DocumentDbProject = (Join-Path $DocumentDbPath "Latency.DocumentDB.sfproj")
	Deploy-Application $DocumentDbPath $DocumentDbProject
}

if ($EventHub -or $All)
{
	$EventHubPath = (Join-Path $SrcFolder "EventHub/Latency.EventHub")
	$EventHubProject = (Join-Path $EventHubPath "Latency.EventHub.sfproj")
	Deploy-Application $EventHubPath $EventHubProject
}

if ($Redis -or $All)
{
	$RedisPath = (Join-Path $SrcFolder "Redis/Latency.Redis")
	$RedisProject = (Join-Path $RedisPath "Latency.Redis.sfproj")
	Deploy-Application $RedisPath $RedisProject
}

if ($ServiceFabric -or $All)
{
	$ServiceFabricPath = (Join-Path $SrcFolder "ServiceFabric/Latency.App")
	$ServiceFabricProject = (Join-Path $ServiceFabricPath "Latency.App.sfproj")
	Deploy-Application $ServiceFabricPath $ServiceFabricProject
}

if ($Sql -or $All)
{
	$SqlPath = (Join-Path $SrcFolder "SQL/Latency.SQL")
	$SqlProject = (Join-Path $SqlPath "Latency.SQL.sfproj")
	Deploy-Application $SqlPath $SqlProject
}

if ($Storage -or $All)
{
	$StoragePath = (Join-Path $SrcFolder "Storage/Latency.Storage")
	$StorageProject = (Join-Path $StoragePath "Latency.Storage.sfproj")
	Deploy-Application $StoragePath $StorageProject
}

if ($Telemetry -or $All)
{
	$TelemetryPath = (Join-Path $SrcFolder "Telemetry/Latency.Telemetry")
	$TelemetryProject = (Join-Path $TelemetryPath "Latency.Telemetry.sfproj")
	Deploy-Application $TelemetryPath $TelemetryProject
}

if ($Web -or $All)
{
	$WebPath = (Join-Path $SrcFolder "Web/Latency.Web")
	$WebProject = (Join-Path $WebPath "Latency.Web.sfproj")
	Deploy-Application $WebPath $WebProject
}
