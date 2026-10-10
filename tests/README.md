# OpsFlow automated tests

## Unit and integration tests

Run all automated tests from the repository root:

```powershell
dotnet test OpsFlow.sln
```

`OpsFlow.UnitTests` tests the state machine and application/authentication services with mocked repositories. `OpsFlow.IntegrationTests` starts a disposable PostgreSQL container with Testcontainers, starts the real API in `WebApplicationFactory`, and exercises authentication, incident workflow, and role authorization over HTTP. Docker Desktop must be running for integration tests.

The integration-test API uses an isolated PostgreSQL container and a test-only JWT signing key. It replaces the RabbitMQ publisher with a no-op implementation because message delivery is outside the HTTP/database scenarios covered here.

## k6 load test

Start the local Compose API and ensure it responds at `http://localhost:5153/api/health`. Then run k6 with Docker:

```powershell
$loadTestPath = (Resolve-Path .\tests\OpsFlow.LoadTests).Path
docker run --rm --add-host=host.docker.internal:host-gateway -v "${loadTestPath}:/scripts" grafana/k6 run /scripts/load-test.js
```

The test runs three 30-second scenarios concurrently: 100 virtual users list services, 50 list incidents, and 10 create incidents. It authenticates with the seeded local-development admin account. The POST scenario creates records, so use a disposable local database when collecting performance measurements. Credentials can be overridden with `ADMIN_EMAIL` and `ADMIN_PASSWORD`; the target can be set with `BASE_URL`.

The k6 summary reports request counts/rates, error rates, and latency percentiles (p50, p90, p95, and p99) per operation. A scenario fails its threshold if more than 1% of its requests fail.
