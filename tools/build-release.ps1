param(
    [switch]$SkipVerify,
    [switch]$NoZip
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$projectPath = Join-Path $repoRoot "src\HylianGrimoire\HylianGrimoire.csproj"
$projectDir = Split-Path -Parent $projectPath
$propsPath = Join-Path $repoRoot "Directory.Build.props"
$artifactsRoot = Join-Path $repoRoot "artifacts\release"
$runtime = "win-x64"
$configuration = "Release"

function Invoke-Checked {
    param(
        [string]$Name,
        [scriptblock]$Command
    )

    Write-Host ""
    Write-Host "==> $Name" -ForegroundColor Cyan
    & $Command
    $exitCode = $LASTEXITCODE
    if ($exitCode -ne 0) {
        throw "$Name failed with exit code $exitCode."
    }
}

function Get-XmlProperty {
    param(
        [xml]$Xml,
        [string]$Name
    )

    $node = $Xml.Project.PropertyGroup | ForEach-Object { $_.$Name } | Where-Object { $_ } | Select-Object -First 1
    if ($null -eq $node) {
        throw "Directory.Build.props does not define $Name."
    }

    return [string]$node
}

function Get-GitValue {
    param([string[]]$Arguments)

    $value = & git -C $repoRoot @Arguments 2>$null
    if ($LASTEXITCODE -ne 0) {
        return $null
    }

    return ($value | Select-Object -First 1)
}

Push-Location $repoRoot
try {
    [xml]$props = Get-Content -LiteralPath $propsPath -Raw
    [xml]$project = Get-Content -LiteralPath $projectPath -Raw
    $displayName = Get-XmlProperty $props "AppDisplayName"
    $assemblyName = Get-XmlProperty $props "AppAssemblyName"
    $version = Get-XmlProperty $props "AppVersion"
    $targetFramework = Get-XmlProperty $project "TargetFramework"
    $artifactName = "$($displayName -replace '\s+', '-')-$version-$runtime"
    $buildOutputDir = Join-Path $projectDir "bin\$configuration\$targetFramework\$runtime"
    $publishDir = Join-Path $artifactsRoot $artifactName
    $zipPath = Join-Path $artifactsRoot "$artifactName.zip"
    $checksumPath = "$zipPath.sha256"

    if (-not $SkipVerify) {
        Invoke-Checked "verify" { & (Join-Path $PSScriptRoot "verify.cmd") }
    }

    if (Test-Path -LiteralPath $publishDir) {
        Remove-Item -LiteralPath $publishDir -Recurse -Force
    }

    if (Test-Path -LiteralPath $zipPath) {
        Remove-Item -LiteralPath $zipPath -Force
    }

    if (Test-Path -LiteralPath $checksumPath) {
        Remove-Item -LiteralPath $checksumPath -Force
    }

    New-Item -ItemType Directory -Path $publishDir -Force | Out-Null

    Invoke-Checked "dotnet build app" {
        dotnet build $projectPath `
            -c $configuration `
            -r $runtime `
            --self-contained false `
            --nologo
    }

    if (-not (Test-Path -LiteralPath $buildOutputDir)) {
        throw "Expected build output was not found: $buildOutputDir"
    }

    Copy-Item -Path (Join-Path $buildOutputDir "*") -Destination $publishDir -Recurse -Force
    $requiredFiles = @(
        "$assemblyName.exe",
        "$assemblyName.dll",
        "$assemblyName.pri",
        "App.xbf",
        "MainWindow.xbf"
    )
    foreach ($fileName in $requiredFiles) {
        $path = Join-Path $publishDir $fileName
        if (-not (Test-Path -LiteralPath $path)) {
            throw "Release output is missing required WinUI file: $fileName"
        }
    }

    $commit = Get-GitValue @("rev-parse", "--short", "HEAD")
    $dirty = $false
    & git -C $repoRoot diff --quiet -- 2>$null
    if ($LASTEXITCODE -ne 0) {
        $dirty = $true
    }

    $buildInfo = @(
        "$displayName $version",
        "Configuration: $configuration",
        "Runtime: $runtime",
        "Self-contained: false",
        "Source: dotnet build output",
        "Built UTC: $([DateTime]::UtcNow.ToString("yyyy-MM-dd HH:mm:ss"))",
        "Commit: $(if ($commit) { $commit } else { "unknown" })",
        "Working tree dirty: $dirty",
        "Verify: $(if ($SkipVerify) { "skipped" } else { "passed" })"
    )
    Set-Content -LiteralPath (Join-Path $publishDir "build-info.txt") -Value $buildInfo -Encoding UTF8

    if (-not $NoZip) {
        Compress-Archive -Path (Join-Path $publishDir "*") -DestinationPath $zipPath -Force
        $hash = Get-FileHash -LiteralPath $zipPath -Algorithm SHA256
        Set-Content -LiteralPath $checksumPath -Value "$($hash.Hash.ToLowerInvariant())  $([IO.Path]::GetFileName($zipPath))" -Encoding ASCII
    }

    Write-Host ""
    Write-Host "Release build created:" -ForegroundColor Green
    Write-Host "  $publishDir"
    if (-not $NoZip) {
        Write-Host "  $zipPath"
        Write-Host "  $checksumPath"
    }
}
finally {
    Pop-Location
}
