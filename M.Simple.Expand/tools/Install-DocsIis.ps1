[CmdletBinding()]
param(
    [string]$SiteName = 'M.Simple.Expand.Docs',
    [ValidateRange(1024, 65535)][int]$Port = 8091,
    [string]$SitePath = ''
)

$ErrorActionPreference = 'Stop'
if (-not $SitePath) { $SitePath = Join-Path (Split-Path $PSScriptRoot -Parent) 'website' }
$reportPath = Join-Path (Split-Path $PSScriptRoot -Parent) 'artifacts/iis-deployment.json'
[System.IO.Directory]::CreateDirectory((Split-Path $reportPath -Parent)) | Out-Null
$result = @{ Success = $false; SiteName = $SiteName; Port = $Port; Url = "http://127.0.0.1:$Port/"; PhysicalPath = $SitePath }
try {
    $identity = [System.Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = [System.Security.Principal.WindowsPrincipal]::new($identity)
    if (-not $principal.IsInRole([System.Security.Principal.WindowsBuiltInRole]::Administrator)) {
        throw 'Administrator privileges are required to create the IIS site. Run this script in an elevated Windows PowerShell.'
    }
    Import-Module WebAdministration
    $resolvedPath = (Resolve-Path -LiteralPath $SitePath).Path
    if (-not (Test-Path -LiteralPath (Join-Path $resolvedPath 'index.html'))) { throw 'Build website/index.html before deployment.' }
    $poolName = $SiteName + '.Static'
    $existing = Get-Website | Where-Object { $_.Name -eq $SiteName }
    if ($existing) {
        if ([System.IO.Path]::GetFullPath([Environment]::ExpandEnvironmentVariables($existing.PhysicalPath)).TrimEnd('\') -ne $resolvedPath.TrimEnd('\')) {
            throw 'An existing site with this name points to another directory. It has not been changed.'
        }
        if (-not ($existing.Bindings.Collection | Where-Object { $_.protocol -eq 'http' -and $_.bindingInformation -eq "127.0.0.1:${Port}:" })) {
            throw 'The existing site uses different bindings. It has not been changed.'
        }
        $poolName = [string]$existing.ApplicationPool
    } else {
        if (Get-WebBinding | Where-Object { $_.bindingInformation -match (':' + $Port + ':') }) {
            throw 'The requested IIS port is already bound. Existing sites have not been changed.'
        }
        if (Test-Path -LiteralPath "IIS:\AppPools\$poolName") { throw 'An application pool with the selected name already exists. It has not been changed.' }
        New-WebAppPool -Name $poolName | Out-Null
        Set-ItemProperty "IIS:\AppPools\$poolName" -Name managedRuntimeVersion -Value ''
        New-Website -Name $SiteName -Port $Port -IPAddress '127.0.0.1' -PhysicalPath $resolvedPath -ApplicationPool $poolName | Out-Null
    }
    & icacls.exe $resolvedPath /grant ("IIS AppPool\{0}:(OI)(CI)(RX)" -f $poolName) /T /C | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Unable to grant read access to the website application pool.' }
    if ((Get-Service W3SVC).Status -ne 'Running') { Start-Service W3SVC }
    if ((Get-WebAppPoolState -Name $poolName).Value -ne 'Started') { Start-WebAppPool -Name $poolName }
    if ((Get-Website | Where-Object { $_.Name -eq $SiteName }).State -ne 'Started') { Start-Website -Name $SiteName }
    $response = Invoke-WebRequest -Uri "http://127.0.0.1:$Port/" -UseBasicParsing -TimeoutSec 15
    if ($response.StatusCode -ne 200) { throw ('Unexpected HTTP response: ' + $response.StatusCode) }
    $result.Success = $true
    $result.PhysicalPath = $resolvedPath
    $result.ApplicationPool = $poolName
    $result.HttpStatus = $response.StatusCode
} catch {
    $result.Error = $_.Exception.Message
} finally {
    $result | ConvertTo-Json | Set-Content -LiteralPath $reportPath -Encoding UTF8
}
if (-not $result.Success) { Write-Error $result.Error; exit 1 }
Write-Output ('IIS website ready: ' + $result.Url)
