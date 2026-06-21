$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot # script directory, need to go up to repo root
# smoke-test.ps1 is at .opencode/skills/smoke-test/scripts/smoke-test.ps1
# Repo root is 4 levels up from the script
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..\..\..\..\')
$logDir = Join-Path $env:TEMP 'opencode-smoke'
$null = New-Item -ItemType Directory -Path $logDir -Force

$projects = @(
    @{ Name = 'UI.Web';     Path = 'PollyDemo.UI.Web';                          Port = 5062; IsWeb = $true }
    @{ Name = 'Auth.Api';   Path = 'PollyDemo.Auth.Api';                      Port = 5047; IsWeb = $true }
    @{ Name = 'UI.Console'; Path = 'PollyDemo\PollyDemo.UI.Console';     Port = $null; IsWeb = $false }
)

$failed = $false

foreach ($p in $projects) {
    Write-Host ''
    Write-Host "=== Smoke testing $($p.Name) ==="

    if ($p.IsWeb) {
        $logFile = Join-Path $logDir "$($p.Name).log"
        $errFile = Join-Path $logDir "$($p.Name)-err.log"
        $projPath = Join-Path $repoRoot $p.Path

        $proc = Start-Process -PassThru -NoNewWindow -FilePath 'dotnet' `
            -ArgumentList "run", "--project", "`"$projPath`"", "--no-build", "-lp", "http" `
            -RedirectStandardOutput $logFile -RedirectStandardError $errFile

        Write-Host "  Waiting 8s for startup..."
        Start-Sleep 8

        if ($proc.HasExited) {
            Write-Host "  FAIL: $($p.Name) exited prematurely with code $($proc.ExitCode)"
            if (Test-Path $errFile) { Get-Content $errFile -Tail 20 | ForEach-Object { Write-Host "  $_" } }
            if (Test-Path $logFile) { Get-Content $logFile -Tail 10 | ForEach-Object { Write-Host "  $_" } }
            $failed = $true
        } else {
            try {
                $response = Invoke-WebRequest -Uri "http://localhost:$($p.Port)/health" -TimeoutSec 5 -UseBasicParsing
                if ($response.StatusCode -eq 200) {
                    Write-Host "  PASS: $($p.Name) started, health check returned 200"
                } else {
                    Write-Host "  FAIL: $($p.Name) health check returned $($response.StatusCode)"
                    $failed = $true
                }
            } catch {
                Write-Host "  FAIL: $($p.Name) health check failed: $($_.Exception.Message)"
                if (Test-Path $logFile) { Get-Content $logFile -Tail 20 | ForEach-Object { Write-Host "  $_" } }
                $failed = $true
            }
            Stop-Process $proc -Force -ErrorAction SilentlyContinue
            Write-Host "  Stopped $($p.Name)"
        }
    } else {
        $projPath = Join-Path $repoRoot $p.Path
        Write-Host "  Running console app (no args, should show usage and exit 0)..."
        $output = & dotnet run --project "`"$projPath`"" --no-build 2>&1
        $output | ForEach-Object { Write-Host "  $_" }
        if ($LASTEXITCODE -ne 0) {
            Write-Host "  FAIL: $($p.Name) exited with code $LASTEXITCODE"
            $failed = $true
        } else {
            Write-Host "  PASS: $($p.Name) ran successfully (exit 0)"
        }
    }
}

Write-Host ''
if ($failed) {
    Write-Host '!!! SMOKE TEST FAILED !!!'
    exit 1
}
Write-Host 'All smoke tests passed'
exit 0
