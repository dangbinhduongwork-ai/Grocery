using FluentValidation;
using GroceryHub.Api.Filters;
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

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:DefaultConnection", _connectionString);

        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _connectionString,
                ["GroceryHub:ServiceName"] = "GroceryHub.Api",
                ["GroceryHub:EnvironmentName"] = "Testing",
                ["GroceryHub:Version"] = "1.0.0",
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
                app.Use(async (context, middlewareNext) =>
                {
                    context.Response.OnStarting(() =>
                    {
                        if (context.Response.StatusCode >= StatusCodes.Status400BadRequest &&
                            context.Response.StatusCode != StatusCodes.Status503ServiceUnavailable &&
                            context.Response.ContentType is not null &&
                            context.Response.ContentType.StartsWith("application/json", StringComparison.OrdinalIgnoreCase))
                        {
                            context.Response.ContentType = "application/problem+json; charset=utf-8";
                        }

                        return Task.CompletedTask;
                    });

                    await middlewareNext();
                });

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
