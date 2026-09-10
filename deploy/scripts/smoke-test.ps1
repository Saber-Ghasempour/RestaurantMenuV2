[CmdletBinding()]
param(
    [Parameter(Mandatory)] [uri] $BaseUri,
    [ValidateRange(1, 60)] [int] $Attempts = 12,
    [ValidateRange(1, 60)] [int] $DelaySeconds = 5
)

$ErrorActionPreference = 'Stop'
$base = $BaseUri.AbsoluteUri.TrimEnd('/')

foreach ($path in @('/health/live', '/health/ready')) {
    $lastError = $null
    for ($attempt = 1; $attempt -le $Attempts; $attempt++) {
        try {
            $response = Invoke-RestMethod -Uri "$base$path" -TimeoutSec 10
            if ($response.status -eq 'Healthy') {
                $lastError = $null
                break
            }

            $lastError = "Unexpected status '$($response.status)'."
        }
        catch {
            $lastError = $_.Exception.Message
        }

        if ($attempt -lt $Attempts) {
            Start-Sleep -Seconds $DelaySeconds
        }
    }

    if ($lastError) {
        throw "Smoke test failed for $path after $Attempts attempts: $lastError"
    }
}

Write-Output 'Liveness and readiness smoke tests passed.'
