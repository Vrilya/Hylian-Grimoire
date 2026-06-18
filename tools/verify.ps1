param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$solutionPath = Join-Path $repoRoot "HylianGrimoire.slnx"

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

function Test-TextHygiene {
    Write-Host ""
    Write-Host "==> Text hygiene" -ForegroundColor Cyan

    $textExtensions = @(
        ".cs",
        ".csproj",
        ".cmd",
        ".editorconfig",
        ".gitignore",
        ".h",
        ".manifest",
        ".md",
        ".mjs",
        ".props",
        ".ps1",
        ".slnx",
        ".txt",
        ".xaml"
    )
    $extensionlessTextFiles = @("LICENSE")
    $utf8Strict = [System.Text.UTF8Encoding]::new($false, $true)
    $problems = New-Object System.Collections.Generic.List[string]
    $checked = New-Object System.Collections.Generic.List[string]

    $trackedFiles = & git -C $repoRoot ls-files --cached --others --exclude-standard
    if ($LASTEXITCODE -ne 0) {
        throw "git ls-files failed. Text hygiene verification needs a Git checkout."
    }

    foreach ($relativePath in $trackedFiles) {
        $extension = [IO.Path]::GetExtension($relativePath)
        if (($textExtensions -notcontains $extension) -and ($extensionlessTextFiles -notcontains $relativePath)) {
            continue
        }

        $path = Join-Path $repoRoot $relativePath
        if (-not (Test-Path -LiteralPath $path)) {
            continue
        }

        [void]$checked.Add($relativePath)
        $bytes = [IO.File]::ReadAllBytes($path)
        if ($bytes.Length -eq 0) {
            continue
        }

        if (($bytes.Length -ge 3) -and ($bytes[0] -eq 0xEF) -and ($bytes[1] -eq 0xBB) -and ($bytes[2] -eq 0xBF)) {
            $problems.Add("${relativePath}: has a UTF-8 BOM")
        }

        if (($bytes.Length -ge 2) -and ((($bytes[0] -eq 0xFF) -and ($bytes[1] -eq 0xFE)) -or (($bytes[0] -eq 0xFE) -and ($bytes[1] -eq 0xFF)))) {
            $problems.Add("${relativePath}: has a UTF-16 BOM")
        }

        try {
            [void]$utf8Strict.GetString($bytes)
        }
        catch {
            $problems.Add("${relativePath}: is not valid UTF-8")
        }

        $hasBareLf = $false
        $hasBareCr = $false
        for ($i = 0; $i -lt $bytes.Length; $i++) {
            if (($bytes[$i] -eq 0x0A) -and (($i -eq 0) -or ($bytes[$i - 1] -ne 0x0D))) {
                $hasBareLf = $true
                break
            }
        }

        for ($i = 0; $i -lt $bytes.Length; $i++) {
            if (($bytes[$i] -eq 0x0D) -and (($i + 1 -ge $bytes.Length) -or ($bytes[$i + 1] -ne 0x0A))) {
                $hasBareCr = $true
                break
            }
        }

        if ($hasBareLf) {
            $problems.Add("${relativePath}: uses LF instead of CRLF")
        }

        if ($hasBareCr) {
            $problems.Add("${relativePath}: contains bare CR line endings")
        }

        if ($bytes[$bytes.Length - 1] -ne 0x0A) {
            $problems.Add("${relativePath}: is missing a final newline")
        }

        if ($extension -ne ".md") {
            $lineStart = 0
            for ($i = 0; $i -lt $bytes.Length; $i++) {
                if ($bytes[$i] -ne 0x0A) {
                    continue
                }

                $lineEnd = $i - 1
                if (($lineEnd -ge 0) -and ($bytes[$lineEnd] -eq 0x0D)) {
                    $lineEnd--
                }

                if (($lineEnd -ge $lineStart) -and (($bytes[$lineEnd] -eq 0x20) -or ($bytes[$lineEnd] -eq 0x09))) {
                    $problems.Add("${relativePath}: contains trailing whitespace")
                    break
                }

                $lineStart = $i + 1
            }
        }
    }

    if ($problems.Count -gt 0) {
        $problems | ForEach-Object { Write-Host "  $_" -ForegroundColor Yellow }
        throw "Text hygiene verification failed with $($problems.Count) issue(s)."
    }

    Write-Host "Text hygiene OK ($($checked.Count) files)."
}

Push-Location $repoRoot
try {
    Test-TextHygiene
    Invoke-Checked "dotnet format" { dotnet format $solutionPath --verify-no-changes --verbosity minimal }
    Invoke-Checked "dotnet build ($Configuration)" { dotnet build $solutionPath -c $Configuration }
    Invoke-Checked "dotnet test ($Configuration)" { dotnet test $solutionPath -c $Configuration --no-build }
}
finally {
    Pop-Location
}

Write-Host ""
Write-Host "Verify completed successfully." -ForegroundColor Green
