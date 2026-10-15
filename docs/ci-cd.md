# OpsFlow CI

The GitHub Actions workflow is `.github/workflows/ci.yml`. It runs on pushes to `main` or `master`, pull requests, and manual dispatches.

## What it checks

1. Restores and tests every project included in `OpsFlow.sln`.
2. Separately restores and builds `tests/OpsFlow.RedisBenchmarkHost`, a .NET project in the repository that is not included in the solution.
3. Validates the root Docker Compose configuration.
4. Builds the API, Worker, and web images from the Dockerfiles referenced by Compose.
5. Starts the complete Compose stack on a uniquely named, disposable GitHub-hosted runner. It checks the API health endpoint, the web page, and that the Worker begins consuming from RabbitMQ.
6. Removes that run's Compose containers, network, and PostgreSQL volume in an unconditional cleanup step.

The API and Worker Dockerfiles live in their respective project folders because each describes how to package one application. The root `docker-compose.yml` is the local stack definition: it builds API, Worker, and web; it pulls PostgreSQL, Redis, and RabbitMQ images; and it connects all six services on the Compose network.

## What it does not do

- It does not use AWS, publish to ECR or GHCR, or deploy to Kubernetes or another hosting service.
- It does not scan source or container images for known security vulnerabilities.
- A successful workflow means the code and Compose stack passed these checks on GitHub's runner. It does not mean OpsFlow is hosted online.
- It does not run k6 load tests or scan the upstream PostgreSQL, Redis, and RabbitMQ images.

There is no configured delivery destination in the current project, so the workflow is scoped as CI. Publishing images requires choosing and configuring a registry separately.

## Run the stack locally

From the repository root:

```powershell
docker compose up --build
```

Compose exposes the web UI at `http://localhost:5174`, the API at `http://localhost:5153`, PostgreSQL at host port `5433`, Redis at `6380`, and RabbitMQ at `5673` (management UI at `15673`). Containers use Compose service names such as `postgres`, `redis`, `rabbitmq`, and `api` to connect to each other.

The normal local PostgreSQL data is stored in the named volume `postgres_data`. `docker compose down` keeps that volume; `docker compose down --volumes` deletes it. The CI cleanup uses a unique Compose project name on a disposable GitHub runner, so it does not target a developer's local database.
