FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files first so Docker can reuse the restore layer when only source changes.
COPY OpsFlow.Domain/OpsFlow.Domain.csproj OpsFlow.Domain/
COPY OpsFlow.Application/OpsFlow.Application.csproj OpsFlow.Application/
COPY OpsFlow.Infrastructure/OpsFlow.Infrastructure.csproj OpsFlow.Infrastructure/
COPY OpsFlow.Worker/OpsFlow.Worker.csproj OpsFlow.Worker/

RUN dotnet restore OpsFlow.Worker/OpsFlow.Worker.csproj

COPY . .
RUN dotnet publish OpsFlow.Worker/OpsFlow.Worker.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS final
WORKDIR /app

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "OpsFlow.Worker.dll"]
