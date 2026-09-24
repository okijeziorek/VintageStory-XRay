param(
    [string]$VintageStoryPath,
    [switch]$SkipInstall
)

$ErrorActionPreference = 'Stop'
$repoRoot = $PSScriptRoot
$projectPath = Join-Path $repoRoot 'src\VintageStoryXRay\VintageStoryXRay.csproj'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'

function Find-VintageStoryPath {
    param([string]$RequestedPath)

    $candidates = [System.Collections.Generic.List[string]]::new()
    if ($RequestedPath) { $candidates.Add($RequestedPath) }
    if ($env:VINTAGE_STORY) { $candidates.Add($env:VINTAGE_STORY) }
    $candidates.Add((Join-Path $env:APPDATA 'Vintagestory'))
    $candidates.Add((Join-Path $env:LOCALAPPDATA 'Vintagestory'))
    $candidates.Add((Join-Path $env:ProgramFiles 'Vintage Story'))
    if (${env:ProgramFiles(x86)}) { $candidates.Add((Join-Path ${env:ProgramFiles(x86)} 'Steam\steamapps\common\Vintage Story')) }

    foreach ($candidate in ($candidates | Select-Object -Unique)) {
        if ((Test-Path (Join-Path $candidate 'VintagestoryAPI.dll')) -and
            (Test-Path (Join-Path $candidate 'VintagestoryLib.dll')) -and
            (Test-Path (Join-Path $candidate 'Lib\0Harmony.dll'))) {
            return (Resolve-Path $candidate).Path
        }
    }

    while ($true) {
        $entered = Read-Host 'Vintage Story installation folder (the folder containing VintagestoryAPI.dll)'
        if ([string]::IsNullOrWhiteSpace($entered)) { throw 'No Vintage Story folder was provided.' }
        $entered = [Environment]::ExpandEnvironmentVariables($entered.Trim().Trim('"'))
        if ((Test-Path (Join-Path $entered 'VintagestoryAPI.dll')) -and
            (Test-Path (Join-Path $entered 'VintagestoryLib.dll')) -and
            (Test-Path (Join-Path $entered 'Lib\0Harmony.dll'))) {
            return (Resolve-Path $entered).Path
        }
        Write-Host 'That folder must contain VintagestoryAPI.dll, VintagestoryLib.dll, and Lib\0Harmony.dll.' -ForegroundColor Yellow
    }
}

try {
    $vsPath = Find-VintageStoryPath -RequestedPath $VintageStoryPath
    Write-Host "Vintage Story files: $vsPath" -ForegroundColor Cyan

    $dotnetCommand = Get-Command dotnet -ErrorAction SilentlyContinue
    if (-not $dotnetCommand) {
        throw 'The .NET 10 SDK was not found. Install the .NET 10 SDK and run this builder again.'
    }

    $sdkList = & dotnet --list-sdks 2>&1
    if ($LASTEXITCODE -ne 0 -or -not ($sdkList | Where-Object { $_ -match '^10\.' })) {
        throw 'The .NET 10 SDK was not found. Install the .NET 10 SDK and run this builder again.'
    }
    Write-Host "Using .NET SDK: $($sdkList | Where-Object { $_ -match '^10\.' } | Select-Object -Last 1)" -ForegroundColor Cyan

    Push-Location $repoRoot
    try {
        Write-Host 'Running dotnet restore...' -ForegroundColor Cyan
        & dotnet restore $projectPath "-p:VSPath=$vsPath"
        if ($LASTEXITCODE -ne 0) { throw "dotnet restore failed with exit code $LASTEXITCODE." }

        Write-Host 'Running dotnet build -c Release...' -ForegroundColor Cyan
        & dotnet build $projectPath -c Release --no-restore "-p:VSPath=$vsPath"
        if ($LASTEXITCODE -ne 0) { throw "dotnet build failed with exit code $LASTEXITCODE." }
    }
    finally { Pop-Location }

    $zipPath = Join-Path $repoRoot 'dist\VintageStoryXRay-0.1.0-dev.zip'
    if (-not (Test-Path $zipPath)) { throw "Build succeeded, but the mod ZIP was not created: $zipPath" }

    if (-not $SkipInstall) {
        $modsPath = Join-Path $env:APPDATA 'VintagestoryData\Mods'
        New-Item -ItemType Directory -Path $modsPath -Force | Out-Null
        $installedZip = Join-Path $modsPath (Split-Path $zipPath -Leaf)
        Copy-Item -LiteralPath $zipPath -Destination $installedZip -Force
        Write-Host "SUCCESS: build and installation completed." -ForegroundColor Green
        Write-Host "Mod ZIP: $zipPath"
        Write-Host "Installed to: $installedZip"
    }
    else {
        Write-Host 'SUCCESS: build completed; installation was skipped.' -ForegroundColor Green
        Write-Host "Mod ZIP: $zipPath"
    }
}
catch {
    Write-Host "FAILED: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
