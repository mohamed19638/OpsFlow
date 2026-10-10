FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files first so Docker can reuse the restore layer when only source changes.
COPY OpsFlow.Domain/OpsFlow.Domain.csproj OpsFlow.Domain/
COPY OpsFlow.Application/OpsFlow.Application.csproj OpsFlow.Application/
COPY OpsFlow.Infrastructure/OpsFlow.Infrastructure.csproj OpsFlow.Infrastructure/
COPY OpsFlow.Api/OpsFlow.Api.csproj OpsFlow.Api/

RUN dotnet restore OpsFlow.Api/OpsFlow.Api.csproj

COPY . .
RUN dotnet publish OpsFlow.Api/OpsFlow.Api.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "OpsFlow.Api.dll"]
