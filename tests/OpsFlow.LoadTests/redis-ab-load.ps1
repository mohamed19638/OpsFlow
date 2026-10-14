[CmdletBinding()]
param(
    [int]$Vus = 25,
    [string]$Duration = "30s",
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
$loadScriptDirectory = $PSScriptRoot
$composeFile = Join-Path $repoRoot "docker-compose.redis-benchmark.yml"
$runId = Get-Date -Format "yyyyMMdd-HHmmss"
$outputDirectory = Join-Path $env:TEMP "OpsFlowRedisBenchmark/k6-$runId"
New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null

dotnet build $hostProject --configuration Release --no-restore
if ($LASTEXITCODE -ne 0) { throw "Benchmark host build failed." }

$runningServices = & docker compose -p $ComposeProject -f $composeFile ps --status running --services
if ($LASTEXITCODE -ne 0 -or @($runningServices) -notcontains "postgres" -or @($runningServices) -notcontains "redis") {
    throw "Start the isolated benchmark dependencies first using the command in README.redis-ab.md."
}

$previousEnvironment = @{}
foreach ($name in @(
    "ASPNETCORE_ENVIRONMENT", "ConnectionStrings__DefaultConnection", "Redis__ConnectionString",
    "JwtSettings__SecretKey", "JwtSettings__Issuer", "JwtSettings__Audience",
    "JwtSettings__AccessTokenExpirationMinutes", "JwtSettings__RefreshTokenExpirationDays",
    "Benchmark__CacheMode", "CACHE_MODE", "BASE_URL", "VUS", "DURATION", "ADMIN_EMAIL", "ADMIN_PASSWORD"
)) {
    $previousEnvironment[$name] = [Environment]::GetEnvironmentVariable($name, "Process")
}

$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:ConnectionStrings__DefaultConnection = "Host=127.0.0.1;Port=15433;Database=opsflow_redis_benchmark;Username=opsflow_benchmark;Password=opsflow_benchmark_local_only;"
$env:Redis__ConnectionString = "127.0.0.1:16380"
$env:JwtSettings__SecretKey = [Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(48))
$env:JwtSettings__Issuer = "OpsFlow"
$env:JwtSettings__Audience = "OpsFlowClients"
$env:JwtSettings__AccessTokenExpirationMinutes = "60"
$env:JwtSettings__RefreshTokenExpirationDays = "7"
$env:BASE_URL = "http://host.docker.internal:5183"
$env:VUS = [string]$Vus
$env:DURATION = $Duration
$env:ADMIN_EMAIL = $AdminEmail
$env:ADMIN_PASSWORD = $AdminPassword

$http = [System.Net.Http.HttpClient]::new()
$http.Timeout = [TimeSpan]::FromSeconds(5)
$baseUri = [Uri]::new($BaseUrl.TrimEnd('/') + '/')
$results = [System.Collections.Generic.List[object]]::new()

function Start-HostForMode {
    param([ValidateSet("Bypass", "Redis")][string]$Mode, [string]$LogPrefix)
    $env:Benchmark__CacheMode = $Mode
    $process = Start-Process -FilePath "dotnet" `
        -ArgumentList @($hostDll, "--urls", $BaseUrl) `
        -WorkingDirectory $repoRoot -PassThru -WindowStyle Hidden `
        -RedirectStandardOutput "$LogPrefix.api.stdout.log" `
        -RedirectStandardError "$LogPrefix.api.stderr.log"

    for ($attempt = 0; $attempt -lt 90; $attempt++) {
        if ($process.HasExited) { throw "Benchmark API exited during startup; inspect $LogPrefix.api.stderr.log." }
        try {
            $response = $http.GetAsync([Uri]::new($baseUri, "api/health")).GetAwaiter().GetResult()
            if ([int]$response.StatusCode -eq 200) { $response.Dispose(); return $process }
            $response.Dispose()
        } catch { }
        Start-Sleep -Milliseconds 500
    }
    throw "Benchmark API health endpoint did not respond; inspect $LogPrefix.api.stdout.log."
}

function Stop-Host {
    param([System.Diagnostics.Process]$Process)
    if (-not $Process.HasExited) { Stop-Process -Id $Process.Id -Force; $Process.WaitForExit() }
    $Process.Dispose()
}

function Invoke-K6 {
    param([string]$Mode, [int]$Repetition)

    $runName = "$($Mode.ToLowerInvariant())-run$Repetition"
    $summaryPath = Join-Path $outputDirectory "$runName.json"
    $logPrefix = Join-Path $outputDirectory $runName
    $api = Start-HostForMode -Mode $Mode -LogPrefix $logPrefix
    try {
        $env:CACHE_MODE = $Mode
        $dockerArgs = @(
            "run", "--rm", "--add-host", "host.docker.internal:host-gateway",
            "--mount", "type=bind,source=$loadScriptDirectory,target=/scripts,readonly",
            "--mount", "type=bind,source=$outputDirectory,target=/results",
            "-e", "CACHE_MODE", "-e", "BASE_URL", "-e", "VUS", "-e", "DURATION", "-e", "ADMIN_EMAIL", "-e", "ADMIN_PASSWORD", "-e", "RUN_NAME=$runName",
            "grafana/k6:latest", "run", "/scripts/redis-ab-load.js"
        )
        & docker @dockerArgs 2>&1 | Tee-Object -FilePath "$logPrefix.k6.log"
        $dockerExitCode = $LASTEXITCODE
        if (-not (Test-Path $summaryPath)) { throw "k6 did not write $summaryPath. Check $logPrefix.k6.log." }
        if ($dockerExitCode -ne 0) { throw "k6 returned exit code $dockerExitCode. The summary is preserved at $summaryPath." }

        $summary = Get-Content $summaryPath -Raw | ConvertFrom-Json
        $requestValues = $summary.metrics.services_requests.values
        $latency = $summary.metrics.services_latency.values
        $requestCount = [int]$requestValues.PSObject.Properties["count"].Value
        $requestRate = [double]$requestValues.PSObject.Properties["rate"].Value
        $p50 = [double]$latency.PSObject.Properties["p(50)"].Value
        $p95 = [double]$latency.PSObject.Properties["p(95)"].Value
        $p99 = [double]$latency.PSObject.Properties["p(99)"].Value
        $successRate = [double]$summary.metrics.services_success_rate.values.rate
        $logText = Get-Content "$logPrefix.api.stdout.log" -Raw
        $pathEvents = if ($Mode -eq "Bypass") {
            [regex]::Matches($logText, "Benchmark cache BYPASS for key services:org:").Count
        } else {
            [regex]::Matches($logText, "Cache HIT for key services:org:").Count
        }
        if ($pathEvents -lt $requestCount) {
            throw "$Mode path verification failed: k6 measured $requestCount service requests but only $pathEvents matching cache logs were captured."
        }

        $results.Add([pscustomobject]@{
            Scenario = $Mode
            Repetition = $Repetition
            VUs = $Vus
            Duration = $Duration
            Requests = $requestCount
            RequestsPerSecond = [Math]::Round($requestRate, 3)
            ErrorsPercent = [Math]::Round((1 - $successRate) * 100, 4)
            P50Ms = [Math]::Round($p50, 3)
            P95Ms = [Math]::Round($p95, 3)
            P99Ms = [Math]::Round($p99, 3)
            VerifiedPathEvents = $pathEvents
            SummaryJson = $summaryPath
            ApiLog = "$logPrefix.api.stdout.log"
            K6Log = "$logPrefix.k6.log"
        })
        Write-Host "$Mode k6 run ${Repetition}: requests=$requestCount, RPS=$([Math]::Round($requestRate, 2)), errors=$([Math]::Round((1 - $successRate) * 100, 3))%, p50=$([Math]::Round($p50, 2)) ms, p95=$([Math]::Round($p95, 2)) ms, p99=$([Math]::Round($p99, 2)) ms; path events=$pathEvents."
    }
    finally {
        Stop-Host -Process $api
    }
}

try {
    foreach ($mode in @("Bypass", "Redis")) {
        for ($repetition = 1; $repetition -le $Repetitions; $repetition++) {
            Invoke-K6 -Mode $mode -Repetition $repetition
        }
    }
    $summaryCsv = Join-Path $outputDirectory "load-summary.csv"
    $results | Export-Csv -NoTypeInformation -Path $summaryCsv
    Write-Host "Load results: $summaryCsv"
    Write-Host "Detailed JSON and API/k6 logs: $outputDirectory"
}
finally {
    $http.Dispose()
    foreach ($name in $previousEnvironment.Keys) {
        if ($null -eq $previousEnvironment[$name]) {
            [Environment]::SetEnvironmentVariable($name, $null, "Process")
        } else {
            [Environment]::SetEnvironmentVariable($name, $previousEnvironment[$name], "Process")
        }
    }
}
