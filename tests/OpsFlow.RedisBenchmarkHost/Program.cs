using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpsFlow.API.Controllers;
using OpsFlow.API.Middleware;
using OpsFlow.Application;
using OpsFlow.Application.Interfaces;
using OpsFlow.Application.Settings;
using OpsFlow.Infrastructure;
using OpsFlow.Infrastructure.Data;
using OpsFlow.RedisBenchmarkHost;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var cacheMode = builder.Configuration["Benchmark:CacheMode"] ?? "Redis";
if (string.Equals(cacheMode, "Bypass", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.RemoveAll<ICacheService>();
    builder.Services.AddScoped<ICacheService, BenchmarkBypassCacheService>();
}
else if (!string.Equals(cacheMode, "Redis", StringComparison.OrdinalIgnoreCase))
{
    throw new InvalidOperationException(
        "Benchmark:CacheMode must be either Redis or Bypass.");
}

builder.Services
    .AddControllers()
    .AddApplicationPart(typeof(ServicesController).Assembly);
builder.Services.AddHealthChecks();

var jwtSettings = builder.Configuration
    .GetSection("JwtSettings")
    .Get<JwtSettings>() ?? new JwtSettings();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            RoleClaimType = ClaimTypes.Role
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();
app.UseMiddleware<ExceptionHandlingMiddleware>();
await OpsFlowDbSeeder.SeedAsync(app.Services);
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/api/health");
app.Run();

public partial class Program { }
