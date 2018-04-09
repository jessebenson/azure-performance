Param
(
	[Switch] $All,

	[Switch] $DocumentDB,
	[Switch] $EventHub,
	[Switch] $Redis,
	[Switch] $ServiceBus,
	[Switch] $ServiceFabric,
	[Switch] $SQL
)

$LocalFolder = (Split-Path $MyInvocation.MyCommand.Path)
$RootFolder = Split-Path (Split-Path $LocalFolder)
$SrcFolder = Join-Path $RootFolder "src/Throughput"

. "$LocalFolder\lib.ps1"

#
# Build solution.
#

$Solution = (Join-Path $SrcFolder "azure-performance-throughput.sln")
msbuild $Solution /t:Clean /p:Platform=x64 /p:Configuration=Release
msbuild $Solution /p:Platform=x64 /p:Configuration=Release

#
# Package and deploy projects.
#

if ($DocumentDB -or $All)
{
	$DocumentDbPath = (Join-Path $SrcFolder "DocumentDB/Throughput.DocumentDB")
	$DocumentDbProject = (Join-Path $DocumentDbPath "Throughput.DocumentDB.sfproj")
	Deploy-Application $SrcFolder $DocumentDbPath $DocumentDbProject
}

if ($EventHub -or $All)
{
	$EventHubPath = (Join-Path $SrcFolder "EventHub/Throughput.EventHub")
	$EventHubProject = (Join-Path $EventHubPath "Throughput.EventHub.sfproj")
	Deploy-Application $SrcFolder $EventHubPath $EventHubProject
}

if ($Redis -or $All)
{
	$RedisPath = (Join-Path $SrcFolder "Redis/Throughput.Redis")
	$RedisProject = (Join-Path $RedisPath "Throughput.Redis.sfproj")
	Deploy-Application $SrcFolder $RedisPath $RedisProject
}

if ($ServiceBus -or $All)
{
	$ServiceBusPath = (Join-Path $SrcFolder "ServiceBus/Throughput.ServiceBus")
	$ServiceBusProject = (Join-Path $ServiceBusPath "Throughput.ServiceBus.sfproj")
	Deploy-Application $SrcFolder $ServiceBusPath $ServiceBusProject
}

if ($ServiceFabric -or $All)
{
	$ServiceFabricPath = (Join-Path $SrcFolder "ServiceFabric/Throughput.App")
	$ServiceFabricProject = (Join-Path $ServiceFabricPath "Throughput.App.sfproj")
	Deploy-Application $SrcFolder $ServiceFabricPath $ServiceFabricProject
}

if ($Sql -or $All)
{
	$SqlPath = (Join-Path $SrcFolder "SQL/Throughput.SQL")
	$SqlProject = (Join-Path $SqlPath "Throughput.SQL.sfproj")
	Deploy-Application $SrcFolder $SqlPath $SqlProject
}
