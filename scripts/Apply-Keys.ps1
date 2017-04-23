Param(
	[Parameter(Mandatory=$true)] [string]$Path,
	[Parameter(Mandatory=$false)] [string]$SubscriptionId = 'fc4ea3c9-1d30-4f18-b33b-7404e7da0123',
	[Parameter(Mandatory=$false)] [string]$KeyVaultName = 'azure-performance'
)

$ErrorActionPreference = 'Stop'

Set-AzureRmContext -SubscriptionId $SubscriptionId
$Global:KeyVaultName = $KeyVaultName
$Global:CachedSecrets = @{}

function Apply-KeysInFile($source, $target)
{
	$contents = Get-Content $source | Out-String
	$words = [regex]::matches($contents, [regex]"{{Secret:([A-Za-z0-9-_]+)}}")

	foreach ($capture in $words.Captures)
	{
		$keyName = $capture.Groups[1].value

		# Check if we've already seen this secret
		if ($Global:CachedSecrets.ContainsKey($keyName))
		{
			$contents = $contents -replace "{{Secret:$keyName}}", $Global:CachedSecrets[$keyName]
		}
		else
		{
			$secret = Get-AzureKeyVaultSecret -VaultName $Global:KeyVaultName -Name $keyName
			$result = $secret.SecretValueText
			$contents = $contents -replace "{{Secret:$keyName}}", $result
			$Global:CachedSecrets.Add($keyName, $result)
		}
	}

	$Utf8NoBomEncoding = New-Object System.Text.UTF8Encoding($false)
	[System.IO.File]::WriteAllLines($target, $contents, $Utf8NoBomEncoding)

	Write-Host -ForegroundColor "cyan" "Wrote $($target)"
}

$files = Get-ChildItem $Path -Recurse -Filter "*.clean"
foreach ($f in $files)
{
	$TargetFilename = $f.Basename
	$TargetPath = $f.Directory.ToString()
	$Target = $TargetPath + "\" + $TargetFilename
	$Source = $TargetPath + "\" + $f.Name
	Apply-KeysInFile $Source $Target
}
