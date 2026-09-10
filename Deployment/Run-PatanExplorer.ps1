[CmdletBinding()]
param(
    [switch]$Rebuild,
    [switch]$NoBrowser,
    [ValidateRange(1, 65535)]
    [int]$Port = 8080,
    [string]$UnityEditorPath = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Write-Step
{
    param([string]$Message)

    Write-Host "`n==> $Message" -ForegroundColor Cyan
}

function Get-UnityVersion
{
    param([string]$VersionFilePath)

    $versionLine = Get-Content -LiteralPath $VersionFilePath | Where-Object { $_ -match "^m_EditorVersion:\s*" } | Select-Object -First 1
    if ([string]::IsNullOrWhiteSpace($versionLine))
    {
        throw "Unable to read the pinned Unity version from $VersionFilePath."
    }

    return $versionLine.Substring($versionLine.IndexOf(":") + 1).Trim()
}

function Resolve-UnityEditor
{
    param(
        [string]$Version,
        [string]$RequestedPath
    )

    if (![string]::IsNullOrWhiteSpace($RequestedPath))
    {
        if (!(Test-Path -LiteralPath $RequestedPath -PathType Leaf))
        {
            throw "Unity Editor was not found at the requested path: $RequestedPath"
        }

        return (Resolve-Path -LiteralPath $RequestedPath).Path
    }

    $candidatePaths = @(
        (Join-Path $HOME "Unity\Hub\Editor\$Version\Editor\Unity.exe"),
        (Join-Path ${env:ProgramFiles} "Unity\Hub\Editor\$Version\Editor\Unity.exe")
    )

    foreach ($candidatePath in $candidatePaths)
    {
        if (Test-Path -LiteralPath $candidatePath -PathType Leaf)
        {
            return $candidatePath
        }
    }

    throw "Unity $Version was not found. Install it with Web Build Support in Unity Hub, or pass -UnityEditorPath."
}

function Resolve-Python
{
    $pythonCommand = Get-Command "python.exe" -CommandType Application -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($null -ne $pythonCommand)
    {
        return [PSCustomObject]@{
            FilePath = $pythonCommand.Source
            PrefixArguments = @()
        }
    }

    $pythonLauncher = Get-Command "py.exe" -CommandType Application -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($null -ne $pythonLauncher)
    {
        return [PSCustomObject]@{
            FilePath = $pythonLauncher.Source
            PrefixArguments = @("-3")
        }
    }

    throw "Python 3 was not found. Install Python 3 and ensure python.exe or py.exe is available on PATH."
}

function Assert-ModelAvailable
{
    param([string]$ModelPath)

    if (!(Test-Path -LiteralPath $ModelPath -PathType Leaf))
    {
        throw "The Krishna Mandir FBX is missing: $ModelPath`nRun 'git lfs pull' before rebuilding."
    }

    $stream = [System.IO.File]::OpenRead($ModelPath)
    try
    {
        $buffer = New-Object byte[] 64
        $bytesRead = $stream.Read($buffer, 0, $buffer.Length)
        $prefix = [System.Text.Encoding]::ASCII.GetString($buffer, 0, $bytesRead)
    }
    finally
    {
        $stream.Dispose()
    }

    if ($prefix.StartsWith("version https://git-lfs.github.com/spec/v1"))
    {
        throw "The Krishna Mandir FBX is still a Git LFS pointer. Run 'git lfs pull' before rebuilding."
    }
}

function Test-WebBuild
{
    param([string]$BuildPath)

    $requiredFiles = @(
        (Join-Path $BuildPath "index.html"),
        (Join-Path $BuildPath "Build\WebGreybox.loader.js"),
        (Join-Path $BuildPath "Build\WebGreybox.data.br"),
        (Join-Path $BuildPath "Build\WebGreybox.framework.js.br"),
        (Join-Path $BuildPath "Build\WebGreybox.wasm.br")
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

function Test-PatanServer
{
    param([string]$Url)

    try
    {
        $response = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 2
        return $response.StatusCode -eq 200 -and $response.Content -match "<title>Patan Explorer</title>"
    }
    catch
    {
        return $false
    }
}

function Invoke-UnityMethod
{
    param(
        [string]$EditorPath,
        [string]$ProjectPath,
        [string]$MethodName,
        [string]$LogPath
    )

    $arguments = @(
        "-batchmode",
        "-quit",
        "-projectPath", "`"$ProjectPath`"",
        "-buildTarget", "WebGL",
        "-executeMethod", $MethodName,
        "-logFile", "`"$LogPath`""
    )

    $process = Start-Process -FilePath $EditorPath -ArgumentList $arguments -PassThru
    $process.WaitForExit()
    if ($process.ExitCode -ne 0)
    {
        throw "Unity method $MethodName failed with exit code $($process.ExitCode). Read $LogPath."
    }
}

function Open-PatanBrowser
{
    param(
        [string]$Url,
        [bool]$ShouldOpen
    )

    if ($ShouldOpen)
    {
        Start-Process -FilePath $Url
    }
}

try
{
    $repositoryRoot = Split-Path -Parent $PSScriptRoot
    $projectPath = Join-Path $repositoryRoot "Unity\PatanExplorer"
    $versionFilePath = Join-Path $projectPath "ProjectSettings\ProjectVersion.txt"
    $modelPath = Join-Path $projectPath "Assets\Patan\Art\KrishnaMandir\Models\KrishnaMandir.fbx"
    $buildRoot = Join-Path $repositoryRoot "Builds"
    $webBuildPath = Join-Path $buildRoot "WebGreybox"
    $serveScriptPath = Join-Path $repositoryRoot "Deployment\serve_web.py"
    $url = "http://127.0.0.1:$Port"

    if (!(Test-Path -LiteralPath $projectPath -PathType Container))
    {
        throw "Unity project directory was not found: $projectPath"
    }

    if (!(Test-Path -LiteralPath $serveScriptPath -PathType Leaf))
    {
        throw "The Brotli-aware server script was not found: $serveScriptPath"
    }

    $buildAvailable = Test-WebBuild -BuildPath $webBuildPath
    $requiresBuild = $Rebuild -or !$buildAvailable
    $serverAlreadyRunning = Test-PatanServer -Url $url

    if ($requiresBuild -and $serverAlreadyRunning)
    {
        throw "Patan Explorer is already running at $url. Stop that server before rebuilding."
    }

    if (!$serverAlreadyRunning)
    {
        $listeners = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue
        if ($null -ne $listeners)
        {
            throw "Port $Port is already in use by another application. Run this script with a different port, for example -Port 8081."
        }
    }

    if ($requiresBuild)
    {
        Write-Step "Preparing the Unity WebGL build"
        $lockFilePath = Join-Path $projectPath "Temp\UnityLockfile"
        if (Test-Path -LiteralPath $lockFilePath)
        {
            throw "The Unity project appears to be open. Close Unity before rebuilding: $projectPath"
        }

        Assert-ModelAvailable -ModelPath $modelPath
        $unityVersion = Get-UnityVersion -VersionFilePath $versionFilePath
        $resolvedUnityEditor = Resolve-UnityEditor -Version $unityVersion -RequestedPath $UnityEditorPath
        New-Item -ItemType Directory -Force -Path $buildRoot | Out-Null

        Write-Host "Unity: $resolvedUnityEditor"
        Write-Host "Project: $projectPath"
        Write-Host "The current checked-in scene will be built without regeneration." -ForegroundColor Green

        Invoke-UnityMethod -EditorPath $resolvedUnityEditor -ProjectPath $projectPath -MethodName "PatanExplorer.Editor.PatanMilestoneBuilder.BuildCurrentSceneWebRelease" -LogPath (Join-Path $buildRoot "WebGreybox-build.log")
        Write-Step "Validating the public milestone"
        Invoke-UnityMethod -EditorPath $resolvedUnityEditor -ProjectPath $projectPath -MethodName "PatanExplorer.Editor.PatanMilestoneValidator.ValidateMilestone" -LogPath (Join-Path $buildRoot "GreyboxValidation.log")

        if (!(Test-WebBuild -BuildPath $webBuildPath))
        {
            throw "Unity exited successfully, but the expected WebGL files were not found in $webBuildPath."
        }
    }
    else
    {
        Write-Step "Using the existing WebGL build"
        Write-Host "Run with -Rebuild after changing Unity scenes, scripts, or assets."
    }

    if ($serverAlreadyRunning)
    {
        Write-Step "Patan Explorer is already running at $url"
        Open-PatanBrowser -Url $url -ShouldOpen (-not $NoBrowser.IsPresent)
        exit 0
    }

    $python = Resolve-Python
    $serverArguments = @()
    $serverArguments += $python.PrefixArguments
    $serverArguments += @(
        "`"$serveScriptPath`"",
        "--directory", "`"$webBuildPath`"",
        "--port", $Port
    )

    Write-Step "Starting the local WebGL server"
    $serverProcess = Start-Process -FilePath $python.FilePath -ArgumentList $serverArguments -WorkingDirectory $repositoryRoot -NoNewWindow -PassThru
    try
    {
        $serverReady = $false
        for ($attempt = 0; $attempt -lt 40; $attempt++)
        {
            Start-Sleep -Milliseconds 250
            if ($serverProcess.HasExited)
            {
                throw "The local server exited before the game became available."
            }

            if (Test-PatanServer -Url $url)
            {
                $serverReady = $true
                break
            }
        }

        if (!$serverReady)
        {
            throw "The local server did not become available at $url."
        }

        Write-Step "Opening Patan Explorer at $url"
        Open-PatanBrowser -Url $url -ShouldOpen (-not $NoBrowser.IsPresent)
        Write-Host "Press Enter in this window to stop the local server." -ForegroundColor Green
        Read-Host | Out-Null
    }
    finally
    {
        if (!$serverProcess.HasExited)
        {
            Stop-Process -Id $serverProcess.Id -ErrorAction SilentlyContinue
            Wait-Process -Id $serverProcess.Id -ErrorAction SilentlyContinue
        }
    }
}
catch
{
    Write-Host "`nPatan Explorer could not start." -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}
