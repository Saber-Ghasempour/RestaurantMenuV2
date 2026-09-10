param(
    [Parameter(Mandatory = $true)]
    [uri] $BaseUrl,
    [int] $Requests = 100,
    [int] $Concurrency = 10
)

$target = [uri]::new($BaseUrl, "/health/live")
$results = 1..$Requests | ForEach-Object -Parallel {
    $response = Invoke-WebRequest -Uri $using:target -SkipHttpErrorCheck
    [pscustomobject]@{ Status = $response.StatusCode }
} -ThrottleLimit $Concurrency

$failures = @($results | Where-Object Status -ne 200)
if ($failures.Count -gt 0) {
    throw "Load smoke failed: $($failures.Count) of $Requests requests were not HTTP 200."
}

Write-Output "Load smoke passed: $Requests requests at concurrency $Concurrency."
