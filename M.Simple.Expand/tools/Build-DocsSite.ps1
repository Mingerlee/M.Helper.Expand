[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
$OutputEncoding = [Console]::OutputEncoding
$projectRoot = Split-Path $PSScriptRoot -Parent
$artifactsPath = Join-Path $projectRoot 'artifacts'
[System.IO.Directory]::CreateDirectory($artifactsPath) | Out-Null
$sampleProject = Join-Path $projectRoot 'docs/samples/M.Simple.Expand.Samples.csproj'

# 构建库和独立示例，将原有库警告留在日志中。
& dotnet build $sampleProject --configuration Release *> (Join-Path $artifactsPath 'docs-build.log')
if ($LASTEXITCODE -ne 0) { throw 'Build failed. See artifacts/docs-build.log.' }
$sampleOutput = & dotnet run --project $sampleProject --configuration Release --no-build -- all
if ($LASTEXITCODE -ne 0) { throw ('Samples failed: ' + ($sampleOutput -join "`n")) }
$markdown = "# 实际运行结果`n`n由文档构建脚本运行全部 15 个场景生成。示例含断言；未访问真实 SMTP、FTP 或业务数据库。`n`n" + '```text' + "`n" + ($sampleOutput -join "`n") + "`n" + '```' + "`n"
[System.IO.File]::WriteAllText((Join-Path $projectRoot 'docs/sample-output.md'), $markdown, [System.Text.UTF8Encoding]::new($false))
& dotnet run --project $sampleProject --configuration Release --no-build -- --write-api (Join-Path $projectRoot 'docs/api.md')
if ($LASTEXITCODE -ne 0) { throw 'API reference generation failed.' }
& node (Join-Path $PSScriptRoot 'build-docs-site.cjs')
if ($LASTEXITCODE -ne 0) { throw 'Website generation failed.' }
Write-Output ('Website ready: ' + (Join-Path $projectRoot 'website'))
Write-Output 'IIS URL: http://127.0.0.1:8091/'
