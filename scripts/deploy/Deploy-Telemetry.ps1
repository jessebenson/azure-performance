Param
(
	[Switch] $All,

	[Switch] $Telemetry,
	[Switch] $Web
)

$LocalFolder = (Split-Path $MyInvocation.MyCommand.Path)
$RootFolder = Split-Path (Split-Path $LocalFolder)
$SrcFolder = Join-Path $RootFolder "src/Telemetry"

. "$LocalFolder\lib.ps1"

#
# Build solution.
#

$Solution = (Join-Path $SrcFolder "azure-telemetry.sln")
msbuild $Solution /t:Clean /p:Platform=x64 /p:Configuration=Release
msbuild $Solution /p:Platform=x64 /p:Configuration=Release

#
# Package and deploy projects.
#

if ($Telemetry -or $All)
{
	$TelemetryPath = (Join-Path $SrcFolder "TelemetryApp")
	$TelemetryProject = (Join-Path $TelemetryPath "TelemetryApp.sfproj")
	Deploy-Application $TelemetryPath $TelemetryProject
}

if ($Web -or $All)
{
	$WebPath = (Join-Path $SrcFolder "WebApp")
	$WebProject = (Join-Path $WebPath "WebApp.sfproj")
	Deploy-Application $WebPath $WebProject
}
