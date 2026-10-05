using System.Net;
using System.Net.Http.Json;
using FluentValidation;
using Mediator;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using TailorSpadi.Application.Features.SystemInfo;

namespace TailorSpadi.IntegrationTests;

public sealed class ApiTests
{
    // Port 1 is intentionally unavailable. These tests do not require Docker.
    private const string UnavailableDatabase =
        "Host=127.0.0.1;Port=1;Database=unavailable;Username=test;Password=test;Timeout=1;Pooling=false";

    [Fact]
    public async Task PublicMetadataWorksWithoutDatabaseOrTenant()
    {
        await using var factory = new ApiFactory(UnavailableDatabase);
        using HttpClient client = factory.CreateClient();
        using HttpResponseMessage response = await client.GetAsync("/api/system-info");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        SystemInfoResponse? body = await response.Content.ReadFromJsonAsync<SystemInfoResponse>();
        Assert.Equal("TailorSpadi", body?.ApplicationName);
    }

    [Fact]
    public async Task DatabaseFailureDoesNotFailLiveness()
    {
        await using var factory = new ApiFactory(UnavailableDatabase);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage live = await client.GetAsync("/health/live");
        using HttpResponseMessage ready = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, live.StatusCode);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, ready.StatusCode);
        Assert.Equal("Unhealthy", await ready.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task MediatorDispatchesRegisteredQuery()
    {
        await using var factory = new ApiFactory(UnavailableDatabase);
        using IServiceScope scope = factory.Services.CreateScope();

        IMediator mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        SystemInfoResponse result = await mediator.Send(new GetSystemInfoQuery());

        Assert.Equal("TailorSpadi", result.ApplicationName);
    }

    [Fact]
    public async Task PipelineValidationBecomesProblemDetails()
    {
        await using var factory = new ApiFactory(UnavailableDatabase);
        using var configured = factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
                services.AddScoped<IValidator<GetSystemInfoQuery>, RejectingValidator>()));
        using HttpClient client = configured.CreateClient();

        using HttpResponseMessage response = await client.GetAsync("/api/system-info");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        string body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Rejected for pipeline verification.", body);
        Assert.DoesNotContain("stackTrace", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UnknownApiRouteReturns404InsteadOfAFrontend()
    {
        await using var factory = new ApiFactory(UnavailableDatabase);
        using HttpClient client = factory.CreateClient();
        using HttpResponseMessage response = await client.GetAsync("/api/not-found");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private sealed class RejectingValidator : AbstractValidator<GetSystemInfoQuery>
    {
        public RejectingValidator()
        {
            RuleFor(query => query).Must(_ => false).WithMessage("Rejected for pipeline verification.");
        }
    }
}
