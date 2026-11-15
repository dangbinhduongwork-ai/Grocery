using System.Collections.Concurrent;
using FluentValidation;
using GroceryHub.Api.Filters;
using GroceryHub.Modules.Identity.Contracts;
using GroceryHub.Persistence.Outbox;
using GroceryHub.SharedKernel;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace GroceryHub.IntegrationTests;

public sealed record TestEmailRecord(Guid UserId, string Email, string Token);

public sealed class TestEmailService : IEmailService
{
    private readonly ConcurrentBag<TestEmailRecord> _verificationEmails = [];
    private readonly ConcurrentBag<TestEmailRecord> _resetPasswordEmails = [];

    public IReadOnlyCollection<TestEmailRecord> VerificationEmails => _verificationEmails;
    public IReadOnlyCollection<TestEmailRecord> ResetPasswordEmails => _resetPasswordEmails;

    public Task SendVerificationEmailAsync(Guid UserId, string Email, string Token, CancellationToken ct = default)
    {
        _verificationEmails.Add(new TestEmailRecord(UserId, Email, Token));
        return Task.CompletedTask;
    }

    public Task SendPasswordResetEmailAsync(Guid UserId, string Email, string Token, CancellationToken ct = default)
    {
        _resetPasswordEmails.Add(new TestEmailRecord(UserId, Email, Token));
        return Task.CompletedTask;
    }

    public void Reset()
    {
        _verificationEmails.Clear();
        _resetPasswordEmails.Clear();
    }
}

public sealed record TestValidationRequest(string Name, int Quantity);

public sealed class TestValidationRequestValidator : AbstractValidator<TestValidationRequest>
{
    public TestValidationRequestValidator()
    {
        RuleFor(static x => x.Name)
            .NotEmpty()
            .WithMessage("Product name is required.");

        RuleFor(static x => x.Quantity)
            .InclusiveBetween(1, 20)
            .WithMessage("Quantity must be between 1 and 20.");
    }
}

public sealed record TestOrderPlacedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid OrderId,
    long GrandTotalVnd) : IIntegrationEvent
{
    public string EventType => "ordering.test-order-placed.v1";
}

public sealed class TestOrderPlacedIntegrationEventHandler : IIntegrationEventHandler<TestOrderPlacedIntegrationEvent>
{
    private int _invocationCount;
    private readonly List<Guid> _handledOrderIds = [];

    public int InvocationCount => Volatile.Read(ref _invocationCount);

    public IReadOnlyList<Guid> HandledOrderIds
    {
        get
        {
            lock (_handledOrderIds)
            {
                return [.. _handledOrderIds];
            }
        }
    }

    public void Reset()
    {
        Interlocked.Exchange(ref _invocationCount, 0);
        lock (_handledOrderIds)
        {
            _handledOrderIds.Clear();
        }
    }

    public Task<Result> HandleAsync(TestOrderPlacedIntegrationEvent integrationEvent, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        Interlocked.Increment(ref _invocationCount);
        lock (_handledOrderIds)
        {
            _handledOrderIds.Add(integrationEvent.OrderId);
        }

        return Task.FromResult(Result.Success());
    }
}

public sealed class GroceryHubWebApplicationFactory(string connectionString) : WebApplicationFactory<Program>
{
    private readonly string _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));

    public TestOrderPlacedIntegrationEventHandler OrderPlacedHandler { get; } = new();
    public TestEmailService EmailService { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:DefaultConnection", _connectionString);
        builder.UseSetting("RateLimiting:AuthPermitLimitPerMinute", "1000");

        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _connectionString,
                ["GroceryHub:ServiceName"] = "GroceryHub.Api",
                ["GroceryHub:EnvironmentName"] = "Testing",
                ["GroceryHub:Version"] = "1.0.0",
                ["RateLimiting:AuthPermitLimitPerMinute"] = "1000",
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remove the background OutboxDispatcher polling loop so tests invoke OutboxDispatcherProcessor deterministically.
            ServiceDescriptor[] outboxHostedServices = services
                .Where(static d => d.ServiceType == typeof(IHostedService) && d.ImplementationType == typeof(OutboxDispatcher))
                .ToArray();

            foreach (ServiceDescriptor descriptor in outboxHostedServices)
            {
                services.Remove(descriptor);
            }

            // Replace IEmailService with test spy
            ServiceDescriptor[] emailServices = services
                .Where(static d => d.ServiceType == typeof(IEmailService))
                .ToArray();

            foreach (ServiceDescriptor descriptor in emailServices)
            {
                services.Remove(descriptor);
            }

            services.AddSingleton<IEmailService>(EmailService);

            OutboxEventTypeRegistry.Register(typeof(TestOrderPlacedIntegrationEvent), "ordering.test-order-placed.v1");
            services.AddSingleton(OrderPlacedHandler);
            services.AddScoped<IIntegrationEventHandler<TestOrderPlacedIntegrationEvent>>(
                sp => sp.GetRequiredService<TestOrderPlacedIntegrationEventHandler>());

            services.AddScoped<IValidator<TestValidationRequest>, TestValidationRequestValidator>();
            services.AddTransient<IStartupFilter, TestEndpointsStartupFilter>();
        });
    }

    private sealed class TestEndpointsStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return app =>
            {
                next(app);

                if (app.Properties.TryGetValue("__EndpointRouteBuilder", out object? routeBuilderObj) &&
                    routeBuilderObj is IEndpointRouteBuilder endpoints)
                {
                    endpoints.MapPost("/__test/validation", static (TestValidationRequest request) => TypedResults.Ok(request))
                        .AllowAnonymous()
                        .WithRequestValidation<TestValidationRequest>();

                    endpoints.MapGet("/__test/exception", static (HttpContext _) =>
                        throw new InvalidOperationException("Simulated unhandled database secret exception at line 99."))
                        .AllowAnonymous();
                }
            };
        }
    }
}
