$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..')
Push-Location $repoRoot

try {
    $trackedFiles = @(git -c core.quotepath=false ls-files)
    if ($LASTEXITCODE -ne 0) {
        throw 'Failed to enumerate tracked files with git ls-files.'
    }

    $violations = [System.Collections.Generic.List[string]]::new()

    $blockedPathPatterns = @(
        '^KickOffAPI/appsettings(\.[^/\\]+)?\.Local\.json$',
        '^KickOffAPI/\.env$',
        '^KickOffAPI/\.env\.(?!example$).+$',
        '^KickOffClient/\.env$',
        '^KickOffClient/\.env\.(?!example$).+$',
        '^.+\.(pfx|p12|key|snk)$',
        '(^|/)[Pp]ublish[Pp]rofile.*\.xml$'
    )

    foreach ($path in $trackedFiles) {
        foreach ($pattern in $blockedPathPatterns) {
            if ($path -match $pattern) {
                $violations.Add("Tracked secret-bearing file path detected: $path")
                break
            }
        }
    }

    $contentPathspecs = @(
        '.',
        ':(exclude)scripts/check-no-secrets.ps1'
    )

    function Add-GitGrepViolations {
        param(
            [string]$Description,
            [string]$Pattern,
            [string[]]$Pathspecs = @('.')
        )

        $output = @(& git grep -n -I -E -- $Pattern -- @Pathspecs 2>$null)
        if ($LASTEXITCODE -eq 0) {
            foreach ($line in $output) {
                if (-not [string]::IsNullOrWhiteSpace($line)) {
                    if ($line -like 'scripts/check-no-secrets.ps1:*') {
                        continue
                    }

                    $script:violations.Add("$Description detected: $line")
                }
            }

            return
        }

        if ($LASTEXITCODE -eq 1) {
            # "No matches" is expected for a clean repo; clear the native exit
            # code so GitHub Actions does not treat the step as failed.
            $global:LASTEXITCODE = 0
            return
        }

        if ($LASTEXITCODE -gt 1) {
            throw "git grep failed while checking: $Description"
        }
    }

    Add-GitGrepViolations `
        -Description 'Private key block' `
        -Pattern '-----BEGIN (RSA |EC |OPENSSH |DSA )?PRIVATE KEY-----' `
        -Pathspecs $contentPathspecs
    Add-GitGrepViolations `
        -Description 'Publish profile XML content' `
        -Pattern '<publishData|<publishProfile' `
        -Pathspecs $contentPathspecs
    Add-GitGrepViolations `
        -Description 'Azure storage connection string with account key' `
        -Pattern 'DefaultEndpointsProtocol=.*AccountKey=' `
        -Pathspecs ($contentPathspecs + @(
            ':(exclude)KickOffAPI/appsettings.Local.example.json',
            ':(exclude)KickOffAPI/KickOffAPI.Tests/Infrastructure/TestServiceFactory.cs'
        ))

    if ($violations.Count -gt 0) {
        Write-Host 'Secret safety check failed:' -ForegroundColor Red
        foreach ($violation in $violations) {
            Write-Host " - $violation" -ForegroundColor Red
        }

        exit 1
    }

    Write-Host 'Secret safety check passed. No tracked secret files or obvious credentials were detected.' -ForegroundColor Green
    exit 0
}
finally {
    Pop-Location
}
