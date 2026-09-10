[CmdletBinding()]
param(
    [switch]$Prod,
    [string]$SiteId = "",
    [string]$BuildName = "WebGreybox"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Write-Step
{
    param([string]$Message)

    Write-Host "`n==> $Message" -ForegroundColor Cyan
}

function Test-WebBuild
{
    param(
        [string]$BuildPath,
        [string]$BuildName
    )

    $requiredFiles = @(
        (Join-Path $BuildPath "index.html"),
        (Join-Path $BuildPath "Build\$BuildName.loader.js"),
        (Join-Path $BuildPath "Build\$BuildName.data.br"),
        (Join-Path $BuildPath "Build\$BuildName.framework.js.br"),
        (Join-Path $BuildPath "Build\$BuildName.wasm.br")
    )

    foreach ($requiredFile in $requiredFiles)
    {
        if (!(Test-Path -LiteralPath $requiredFile -PathType Leaf))
        {
            return $false
        }
    }

    return $true
}

try
{
    $repositoryRoot = Split-Path -Parent $PSScriptRoot
    $webBuildPath = Join-Path $repositoryRoot "Builds\$BuildName"
    $headersSourcePath = Join-Path $repositoryRoot "Deployment\Netlify\_headers"

    if (!(Get-Command "netlify" -CommandType Application -ErrorAction SilentlyContinue))
    {
        throw "The Netlify CLI was not found on PATH. Install it with 'npm install -g netlify-cli'."
    }

    if (!(Test-Path -LiteralPath $headersSourcePath -PathType Leaf))
    {
        throw "The Netlify headers file was not found: $headersSourcePath"
    }

    if (!(Test-WebBuild -BuildPath $webBuildPath -BuildName $BuildName))
    {
        throw "No complete WebGL build was found at $webBuildPath. Build it first, for example with '.\Run-PatanExplorer.cmd -Rebuild'."
    }

    Write-Step "Copying Netlify response headers into the build output"
    Copy-Item -LiteralPath $headersSourcePath -Destination (Join-Path $webBuildPath "_headers") -Force

    $deployArguments = @("deploy", "--dir", $webBuildPath)
    if ($Prod.IsPresent)
    {
        $deployArguments += "--prod"
    }

    if (![string]::IsNullOrWhiteSpace($SiteId))
    {
        $deployArguments += @("--site", $SiteId)
    }

    Write-Step "Deploying $webBuildPath to Netlify"
    & netlify @deployArguments
    if ($LASTEXITCODE -ne 0)
    {
        throw "netlify deploy failed with exit code $LASTEXITCODE."
    }
}
catch
{
    Write-Host "`nNetlify deployment failed." -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}
