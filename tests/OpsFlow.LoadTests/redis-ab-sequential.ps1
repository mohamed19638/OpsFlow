[CmdletBinding()]
param(
    [int]$Samples = 100,
    [int]$Warmups = 5,
    [int]$Repetitions = 3,
    [string]$ComposeProject = "opsflow-redis-benchmark",
    [string]$BaseUrl = "http://127.0.0.1:5183",
    [string]$AdminEmail = "admin@acmetech.io",
    [string]$AdminPassword = "Admin@1234!"
)

$ErrorActionPreference = "Stop"
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "../..")).Path
$hostProject = Join-Path $repoRoot "tests/OpsFlow.RedisBenchmarkHost/OpsFlow.RedisBenchmarkHost.csproj"
$hostDll = Join-Path $repoRoot "tests/OpsFlow.RedisBenchmarkHost/bin/Release/net8.0/OpsFlow.RedisBenchmarkHost.dll"
$composeFile = Join-Path $repoRoot "docker-compose.redis-benchmark.yml"
$runId = Get-Date -Format "yyyyMMdd-HHmmss"
$outputDirectory = Join-Path $env:TEMP "OpsFlowRedisBenchmark/$runId"
New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null

if ($Samples -lt 30 -or $Repetitions -lt 2 -or $Warmups -lt 1) {
    throw "Use at least 30 samples, 2 repetitions, and 1 warm-up. Defaults are 100 / 3 / 5."
}

dotnet build $hostProject --configuration Release
if ($LASTEXITCODE -ne 0) { throw "Benchmark host build failed." }
if (-not (Test-Path $hostDll)) { throw "Benchmark host DLL was not produced: $hostDll" }

$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:ConnectionStrings__DefaultConnection = "Host=127.0.0.1;Port=15433;Database=opsflow_redis_benchmark;Username=opsflow_benchmark;Password=opsflow_benchmark_local_only;"
$env:Redis__ConnectionString = "127.0.0.1:16380"
$env:JwtSettings__SecretKey = [Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(48))
$env:JwtSettings__Issuer = "OpsFlow"
$env:JwtSettings__Audience = "OpsFlowClients"
$env:JwtSettings__AccessTokenExpirationMinutes = "60"
$env:JwtSettings__RefreshTokenExpirationDays = "7"

$baseUri = [Uri]::new($BaseUrl.TrimEnd('/') + '/')
$http = [System.Net.Http.HttpClient]::new()
$http.BaseAddress = $baseUri
$http.Timeout = [TimeSpan]::FromSeconds(15)
$rawSamples = [System.Collections.Generic.List[object]]::new()
$runSummaries = [System.Collections.Generic.List[object]]::new()

function Invoke-GetServices {
    param([string]$Token, [string]$OrganizationId, [string]$ExpectedHash)

    $request = [System.Net.Http.HttpRequestMessage]::new(
        [System.Net.Http.HttpMethod]::Get,
        "api/organizations/$OrganizationId/services")
    $request.Headers.Authorization = [System.Net.Http.Headers.AuthenticationHeaderValue]::new("Bearer", $Token)

    $watch = [System.Diagnostics.Stopwatch]::StartNew()
    $response = $http.SendAsync($request).GetAwaiter().GetResult()
    $body = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
    $watch.Stop()
    $status = [int]$response.StatusCode
    $hash = [Convert]::ToHexString(
        [Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($body)))
    $response.Dispose()
    $request.Dispose()

    if ($status -ne 200) { throw "Service-list request returned HTTP ${status}: $body" }
    if ($ExpectedHash -and $hash -ne $ExpectedHash) {
        throw "Service-list response changed. Expected hash $ExpectedHash, got $hash."
    }

    return [pscustomobject]@{
        LatencyMs = $watch.Elapsed.TotalMilliseconds
        Status = $status
        PayloadBytes = [Text.Encoding]::UTF8.GetByteCount($body)
        PayloadSha256 = $hash
    }
}

function Invoke-CacheCommand {
    param([string]$Verb, [string]$Key)
    $result = & docker compose -p $ComposeProject -f $composeFile exec -T redis redis-cli $Verb $Key
    if ($LASTEXITCODE -ne 0) { throw "Redis $Verb failed: $result" }
    return (($result | Out-String).Trim())
}

function Wait-ForApi {
    param([System.Diagnostics.Process]$Process)
    $healthUrl = [Uri]::new($baseUri, "api/health").AbsoluteUri
    for ($attempt = 0; $attempt -lt 90; $attempt++) {
        if ($Process.HasExited) { throw "Benchmark host exited during startup. Check its log." }
        try {
            $response = $http.GetAsync($healthUrl).GetAwaiter().GetResult()
            if ([int]$response.StatusCode -eq 200) { $response.Dispose(); return }
            $response.Dispose()
        } catch { }
        Start-Sleep -Milliseconds 500
    }
    throw "Benchmark host did not become healthy at $healthUrl."
}

function Start-BenchmarkHost {
    param([ValidateSet("Bypass", "Redis")][string]$Mode, [int]$Repetition)
    $prefix = Join-Path $outputDirectory "$Mode-run$Repetition"
    $env:Benchmark__CacheMode = $Mode
    $process = Start-Process -FilePath "dotnet" `
        -ArgumentList @($hostDll, "--urls", $BaseUrl) `
        -WorkingDirectory $repoRoot -PassThru -WindowStyle Hidden `
        -RedirectStandardOutput "$prefix.stdout.log" `
        -RedirectStandardError "$prefix.stderr.log"
    Wait-ForApi -Process $process
    return [pscustomobject]@{ Process = $process; Prefix = $prefix }
}

function Stop-BenchmarkHost {
    param([System.Diagnostics.Process]$Process)
    if (-not $Process.HasExited) {
        Stop-Process -Id $Process.Id -Force
        $Process.WaitForExit()
    }
    $Process.Dispose()
    $script:http.Dispose()
    $script:http = [System.Net.Http.HttpClient]::new()
    $script:http.BaseAddress = $script:baseUri
    $script:http.Timeout = [TimeSpan]::FromSeconds(15)
}

function Get-Percentile {
    param([double[]]$Values, [double]$Percentile)
    $sorted = @($Values | Sort-Object)
    $index = ($Percentile / 100) * ($sorted.Count - 1)
    $lower = [Math]::Floor($index)
    $upper = [Math]::Ceiling($index)
    if ($lower -eq $upper) { return [double]$sorted[$lower] }
    $fraction = $index - $lower
    return ([double]$sorted[$lower] * (1 - $fraction)) + ([double]$sorted[$upper] * $fraction)
}

try {
    $composeState = & docker compose -p $ComposeProject -f $composeFile ps --status running --services
    if ($LASTEXITCODE -ne 0 -or @($composeState) -notcontains "postgres" -or @($composeState) -notcontains "redis") {
        throw "Start the isolated benchmark dependencies first: docker compose -p $ComposeProject -f docker-compose.redis-benchmark.yml up -d postgres redis"
    }

    $expectedHash = $null
    $organizationId = $null
    $payloadBytes = $null

    for ($repetition = 1; $repetition -le $Repetitions; $repetition++) {
        $order = if (($repetition % 2) -eq 1) { @("Bypass", "Miss", "Hit") } else { @("Hit", "Miss", "Bypass") }
        foreach ($scenario in $order) {
            $mode = if ($scenario -eq "Bypass") { "Bypass" } else { "Redis" }
            $benchHost = Start-BenchmarkHost -Mode $mode -Repetition $repetition
            try {
                $loginBody = @{ email = $AdminEmail; password = $AdminPassword } | ConvertTo-Json -Compress
                $loginContent = [System.Net.Http.StringContent]::new($loginBody, [Text.Encoding]::UTF8, "application/json")
                $loginResponse = $http.PostAsync([Uri]::new($baseUri, "api/auth/login"), $loginContent).GetAwaiter().GetResult()
                $loginJson = $loginResponse.Content.ReadAsStringAsync().GetAwaiter().GetResult() | ConvertFrom-Json
                if ([int]$loginResponse.StatusCode -ne 200) { throw "Seed admin login failed: $loginJson" }
                $token = [string]$loginJson.accessToken
                $payloadPart = $token.Split('.')[1].Replace('-', '+').Replace('_', '/')
                switch ($payloadPart.Length % 4) { 2 { $payloadPart += '==' } 3 { $payloadPart += '=' } }
                $claims = [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($payloadPart)) | ConvertFrom-Json
                $org = [string]$claims.orgId
                if (-not $organizationId) { $organizationId = $org }
                if ($organizationId -ne $org) { throw "Organization changed between runs; database must stay the same." }
                $loginResponse.Dispose(); $loginContent.Dispose()

                $key = "services:org:$org"
                if ($scenario -eq "Hit") {
                    [void](Invoke-CacheCommand -Verb "DEL" -Key $key)
                    $seeded = Invoke-GetServices -Token $token -OrganizationId $org -ExpectedHash $expectedHash
                    if (-not $expectedHash) { $expectedHash = $seeded.PayloadSha256; $payloadBytes = $seeded.PayloadBytes }
                    if ((Invoke-CacheCommand -Verb "EXISTS" -Key $key) -ne "1") { throw "Hit scenario failed to populate Redis key $key." }
                }

                for ($i = 0; $i -lt $Warmups; $i++) {
                    if ($scenario -eq "Miss") { [void](Invoke-CacheCommand -Verb "DEL" -Key $key) }
                    $warm = Invoke-GetServices -Token $token -OrganizationId $org -ExpectedHash $expectedHash
                    if (-not $expectedHash) { $expectedHash = $warm.PayloadSha256; $payloadBytes = $warm.PayloadBytes }
                }

                $measurements = [System.Collections.Generic.List[object]]::new()
                for ($sample = 1; $sample -le $Samples; $sample++) {
                    if ($scenario -eq "Miss") { [void](Invoke-CacheCommand -Verb "DEL" -Key $key) }
                    $measurement = Invoke-GetServices -Token $token -OrganizationId $org -ExpectedHash $expectedHash
                    $measurements.Add($measurement)
                    $rawSamples.Add([pscustomobject]@{
                        Repetition = $repetition
                        Scenario = $scenario
                        Sample = $sample
                        LatencyMs = [Math]::Round($measurement.LatencyMs, 3)
                        Status = $measurement.Status
                        PayloadBytes = $measurement.PayloadBytes
                        PayloadSha256 = $measurement.PayloadSha256
                    })
                }

                $log = Get-Content "$($benchHost.Prefix).stdout.log" -Raw
                $expectedPathCount = $Samples + $Warmups
                $behaviorCount = switch ($scenario) {
                    "Bypass" { [regex]::Matches($log, "Benchmark cache BYPASS for key $([regex]::Escape($key))").Count }
                    "Miss" { [regex]::Matches($log, "Cache MISS for key $([regex]::Escape($key))").Count }
                    "Hit" { [regex]::Matches($log, "Cache HIT for key $([regex]::Escape($key))").Count }
                }
                if ($behaviorCount -lt $expectedPathCount) {
                    throw "$scenario path verification failed: expected at least $expectedPathCount matching log events, found $behaviorCount."
                }

                $latencies = @($measurements | ForEach-Object { [double]$_.LatencyMs })
                $runSummaries.Add([pscustomobject]@{
                    Repetition = $repetition
                    Scenario = $scenario
                    Samples = $Samples
                    Errors = 0
                    P50Ms = [Math]::Round((Get-Percentile $latencies 50), 3)
                    P95Ms = [Math]::Round((Get-Percentile $latencies 95), 3)
                    P99Ms = [Math]::Round((Get-Percentile $latencies 99), 3)
                    PayloadBytes = $payloadBytes
                    PayloadSha256 = $expectedHash
                    VerifiedCacheEvents = $behaviorCount
                    ApiLog = "$($benchHost.Prefix).stdout.log"
                    ApiErrorLog = "$($benchHost.Prefix).stderr.log"
                })
                Write-Host "$scenario run $repetition complete: p50=$([Math]::Round((Get-Percentile $latencies 50), 2)) ms, p95=$([Math]::Round((Get-Percentile $latencies 95), 2)) ms, p99=$([Math]::Round((Get-Percentile $latencies 99), 2)) ms; verified $behaviorCount cache-path log events."
            }
            finally {
                Stop-BenchmarkHost -Process $benchHost.Process
            }
        }
    }

    $rawPath = Join-Path $outputDirectory "sequential-samples.csv"
    $summaryPath = Join-Path $outputDirectory "sequential-summary.csv"
    $rawSamples | Export-Csv -NoTypeInformation -Path $rawPath
    $runSummaries | Export-Csv -NoTypeInformation -Path $summaryPath
    Write-Host "Organization: $organizationId"
    Write-Host "Payload: $payloadBytes bytes, SHA-256 $expectedHash"
    Write-Host "Detailed samples: $rawPath"
    Write-Host "Per-run percentiles: $summaryPath"
    Write-Host "API logs (including EF SQL and Redis durations): $outputDirectory"
    Write-Host "Redis GET duration excludes JSON deserialization; Redis SET duration is not currently logged."
}
finally {
    $http.Dispose()
    Remove-Item Env:Benchmark__CacheMode -ErrorAction SilentlyContinue
}
