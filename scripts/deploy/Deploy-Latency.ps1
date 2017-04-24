Param
(
	[Switch] $All,

	[Switch] $DocumentDB,
	[Switch] $EventHub,
	[Switch] $Redis,
	[Switch] $ServiceFabric,
	[Switch] $SQL,
	[Switch] $Storage
)

. ".\lib.ps1"

$LocalFolder = (Split-Path $MyInvocation.MyCommand.Path)
$RootFolder = Split-Path (Split-Path $LocalFolder)
$SrcFolder = Join-Path $RootFolder "src/Latency"

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
