using System.Net;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Serilog;
using TailorSpadi.Api.Endpoints;
using TailorSpadi.Api.Errors;
using TailorSpadi.Api.Security;
using TailorSpadi.Api.Tenancy;
using TailorSpadi.Application;
using TailorSpadi.Application.Abstractions;
using TailorSpadi.Application.Behaviors;
using TailorSpadi.Application.Features.SystemInfo;
using TailorSpadi.Application.Options;
using TailorSpadi.Infrastructure;
using TailorSpadi.Infrastructure.Email;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSerilog((services, logger) => logger
    .ReadFrom.Configuration(builder.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console());

builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddMediator(options =>
{
    options.ServiceLifetime = ServiceLifetime.Scoped;
    options.Assemblies = [typeof(GetSystemInfoQuery)];
    options.PipelineBehaviors = [typeof(RequestLoggingBehavior<,>), typeof(ValidationBehavior<,>)];
});

// Refuse to start with a missing or weak signing secret or an empty base domain.
builder.Services.AddOptions<AuthOptions>()
    .Validate(options => AuthOptions.IsAcceptableSigningKey(options.SigningKey),
        "Auth:SigningKey is missing or too weak. Configure a random secret of at least 32 characters.")
    .Validate(options => builder.Environment.IsDevelopment() || options.CookieSecure,
        "Secure refresh cookies are required outside Development.")
    .ValidateOnStart();
builder.Services.AddOptions<TenancyOptions>()
    .Validate(options => !string.IsNullOrWhiteSpace(options.BaseDomain), "Tenancy:BaseDomain is required.")
    .ValidateOnStart();

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddSingleton<IEmailSender, DevelopmentLogEmailSender>();
}
else
{
    builder.Services.AddSingleton<IEmailSender, UnconfiguredEmailSender>();
}

builder.Services.AddTailorSpadiAuthentication();
builder.Services.AddTailorSpadiRateLimiting();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

string[] allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
{
    if (allowedOrigins.Length > 0)
    {
        policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
    }
}));

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    // Preserve the original Host header at the proxy; never consume X-Forwarded-Host.
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.ForwardLimit = 1;

    foreach (string address in builder.Configuration.GetSection("ReverseProxy:KnownProxies").Get<string[]>() ?? [])
    {
        options.KnownProxies.Add(IPAddress.Parse(address));
    }
});

var app = builder.Build();

app.UseForwardedHeaders();
app.UseSerilogRequestLogging();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseCors();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// The tenant comes from the accepted hostname only, and is resolved before authentication
// so a token is always judged against the host that received it.
app.UseMiddleware<TenantResolutionMiddleware>();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapSystemInfoEndpoints();
app.MapAuthEndpoints();
app.MapAdminAuthEndpoints();
app.MapOnboardingEndpoints();
app.MapTenantEndpoints();
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready")
});

app.Run();

// Exposes the entry point to WebApplicationFactory without adding another host.
public partial class Program;
