[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [string[]]$Project,
    [string]$OutputDirectory = (Join-Path $PSScriptRoot 'artifacts/packages'),
    [string]$Configuration = 'Release',
    [switch]$PackOnly,
    [switch]$SaveApiKey
)

$ErrorActionPreference = 'Stop'
$source = 'https://api.nuget.org/v3/index.json'
$keyFile = Join-Path $PSScriptRoot 'artifacts/private/nuget-api-key.txt'

if ($SaveApiKey) {
    if ($env:OS -ne 'Windows_NT') {
        throw 'Saving a key requires Windows DPAPI. On other platforms use NUGET_API_KEY.'
    }
    $secret = Read-Host 'NuGet API Key' -AsSecureString
    if ($secret.Length -eq 0) { throw 'API Key cannot be empty.' }
    New-Item -ItemType Directory -Path (Split-Path $keyFile) -Force | Out-Null
    $secret | ConvertFrom-SecureString | Set-Content -LiteralPath $keyFile -Encoding UTF8
    $secret.Dispose()
    Write-Host 'API Key saved encrypted for the current Windows user (excluded from Git).'
}

if (-not $Project) {
    if ($SaveApiKey) { return }
    throw 'Specify a package project, for example: ./Publish-NuGet.ps1 M.Simple.Caching.Redis'
}

# Resolve every project before packing or publishing anything.
$projectPaths = @($Project | ForEach-Object {
    $candidate = $_
    if (-not [IO.Path]::IsPathRooted($candidate)) {
        $candidate = Join-Path $PSScriptRoot $candidate
    }
    if (Test-Path -LiteralPath $candidate -PathType Container) {
        $files = @(Get-ChildItem -LiteralPath $candidate -Filter '*.csproj' -File)
        if ($files.Count -ne 1) { throw "Expected exactly one csproj in: $candidate" }
        $candidate = $files[0].FullName
    }
    if (-not (Test-Path -LiteralPath $candidate -PathType Leaf) -or
        [IO.Path]::GetExtension($candidate) -ne '.csproj') {
        throw "Package project not found: $candidate"
    }
    (Resolve-Path -LiteralPath $candidate).Path
} | Select-Object -Unique)

$apiKey = $null
if (-not $PackOnly) {
    $apiKey = $env:NUGET_API_KEY
    if ([string]::IsNullOrWhiteSpace($apiKey) -and (Test-Path -LiteralPath $keyFile)) {
        $secret = (Get-Content -LiteralPath $keyFile -Raw).Trim() | ConvertTo-SecureString
        $pointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secret)
        try { $apiKey = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($pointer) }
        finally {
            [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($pointer)
            $secret.Dispose()
        }
    }
    if ([string]::IsNullOrWhiteSpace($apiKey)) {
        throw 'Set NUGET_API_KEY or run ./Publish-NuGet.ps1 -SaveApiKey first.'
    }
}

if (-not [IO.Path]::IsPathRooted($OutputDirectory)) {
    $OutputDirectory = Join-Path $PSScriptRoot $OutputDirectory
}
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$packages = @()
try {
    foreach ($projectPath in $projectPaths) {
        # A fresh directory prevents accidentally publishing stale packages.
        $staging = Join-Path $OutputDirectory ([Guid]::NewGuid().ToString('N'))
        New-Item -ItemType Directory -Path $staging | Out-Null
        try {
            & dotnet pack $projectPath --configuration $Configuration --output $staging --nologo
            if ($LASTEXITCODE -ne 0) { throw "dotnet pack failed: $projectPath" }
            $built = @(Get-ChildItem -LiteralPath $staging -Filter '*.nupkg' -File)
            if ($built.Count -eq 0) { throw "No NuGet package generated: $projectPath" }
            foreach ($file in $built) {
                $destination = Join-Path $OutputDirectory $file.Name
                Move-Item -LiteralPath $file.FullName -Destination $destination -Force
                $packages += $destination
            }
        }
        finally {
            # Only remove the unique staging directory created above, never the output root.
            Remove-Item -LiteralPath $staging -Recurse -Force
        }
    }

    foreach ($package in ($packages | Select-Object -Unique)) {
        if ($PackOnly) {
            Write-Host "Package ready: $package"
            continue
        }
        Write-Host "Publishing: $package"
        & dotnet nuget push $package --source $source --api-key $apiKey --skip-duplicate
        if ($LASTEXITCODE -ne 0) { throw "NuGet push failed: $package" }
    }
}
finally { $apiKey = $null }
