using System.Reflection;
using System.Text.Json;
using GroceryHub.Persistence.Entities;
using GroceryHub.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace GroceryHub.Persistence.Outbox;

public sealed class OutboxDispatcherProcessor
{
    private const int MaxAttempts = 10;
    private const int MaxErrorLength = 2000;

    private readonly GroceryHubDbContext _dbContext;
    private readonly IServiceProvider _serviceProvider;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<OutboxDispatcherProcessor> _logger;

    public OutboxDispatcherProcessor(
        GroceryHubDbContext dbContext,
        IServiceProvider serviceProvider,
        TimeProvider timeProvider,
        ILogger<OutboxDispatcherProcessor>? logger = null)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _logger = logger ?? NullLogger<OutboxDispatcherProcessor>.Instance;
    }

    public async Task<int> ProcessPendingBatchAsync(
        int batchSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (batchSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(batchSize), "Batch size must be greater than zero.");
        }

        List<OutboxMessage> pendingMessages = await _dbContext.OutboxMessages
            .Where(m => m.ProcessedAt == null && m.Attempts < MaxAttempts)
            .OrderBy(m => m.OccurredAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        if (pendingMessages.Count == 0)
        {
            return 0;
        }

        int processedCount = 0;

        foreach (OutboxMessage message in pendingMessages)
        {
            bool succeeded = await ProcessSingleMessageAsync(message, cancellationToken);
            if (succeeded)
            {
                processedCount++;
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return processedCount;
    }

    private async Task<bool> ProcessSingleMessageAsync(
        OutboxMessage message,
        CancellationToken cancellationToken)
    {
        Type? eventClrType = OutboxEventTypeRegistry.ResolveEventType(
            message.EventType,
            message.Payload,
            _serviceProvider);

        if (eventClrType is null)
        {
            message.Attempts += 1;
            message.LastError = Truncate($"Unrecognized integration event type '{message.EventType}'.");
            _logger.LogWarning(
                "Outbox message {MessageId} has unrecognized event type {EventType}",
                message.Id,
                message.EventType);
            return false;
        }

        object? deserializedEvent;
        try
        {
            deserializedEvent = JsonSerializer.Deserialize(
                message.Payload,
                eventClrType,
                OutboxEventTypeRegistry.SerializerOptions);
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException)
        {
            message.Attempts += 1;
            message.LastError = Truncate(ex.Message);
            _logger.LogError(
                ex,
                "Failed to deserialize outbox message {MessageId} of type {EventType}",
                message.Id,
                message.EventType);
            return false;
        }

        if (deserializedEvent is null)
        {
            message.Attempts += 1;
            message.LastError = "Deserialized integration event payload was null.";
            return false;
        }

        Type handlerInterfaceType = typeof(IIntegrationEventHandler<>).MakeGenericType(eventClrType);
        object?[] handlers = _serviceProvider.GetServices(handlerInterfaceType).ToArray();
        MethodInfo? handleMethod = handlerInterfaceType.GetMethod(nameof(IIntegrationEventHandler<IIntegrationEvent>.HandleAsync));

        if (handleMethod is null)
        {
            message.Attempts += 1;
            message.LastError = "Could not locate HandleAsync method on handler interface.";
            return false;
        }

        List<string> existingReceiptHandlers = await _dbContext.OutboxConsumerReceipts
            .AsNoTracking()
            .Where(r => r.MessageId == message.Id)
            .Select(r => r.HandlerName)
            .ToListAsync(cancellationToken);

        HashSet<string> completedHandlers = new(existingReceiptHandlers, StringComparer.Ordinal);

        foreach (object? handler in handlers)
        {
            if (handler is null)
            {
                continue;
            }

            Type handlerType = handler.GetType();
            string handlerFullName = handlerType.FullName ?? handlerType.Name;
            string handlerShortName = handlerType.Name;

            if (completedHandlers.Contains(handlerFullName) || completedHandlers.Contains(handlerShortName))
            {
                continue;
            }

            try
            {
                Task<Result> task = (Task<Result>)handleMethod.Invoke(
                    handler,
                    [deserializedEvent, cancellationToken])!;

                Result result = await task.ConfigureAwait(false);
                if (result.IsFailure)
                {
                    message.Attempts += 1;
                    message.LastError = Truncate($"{result.Error.Code}: {result.Error.Message}");
                    return false;
                }

                DateTimeOffset receiptTime = _timeProvider.GetUtcNow();
                bool alreadyTrackedLocally = _dbContext.OutboxConsumerReceipts.Local.Any(
                    r => r.MessageId == message.Id &&
                         string.Equals(r.HandlerName, handlerFullName, StringComparison.Ordinal));

                if (!alreadyTrackedLocally)
                {
                    _dbContext.OutboxConsumerReceipts.Add(new OutboxConsumerReceipt
                    {
                        MessageId = message.Id,
                        HandlerName = handlerFullName,
                        ProcessedAt = receiptTime,
                    });
                }

                completedHandlers.Add(handlerFullName);
            }
            catch (Exception ex)
            {
                Exception unwrapped = ex is TargetInvocationException tie && tie.InnerException is not null
                    ? tie.InnerException
                    : ex;

                message.Attempts += 1;
                message.LastError = Truncate(unwrapped.Message);
                _logger.LogError(
                    unwrapped,
                    "Error executing handler {HandlerName} for outbox message {MessageId}",
                    handlerFullName,
                    message.Id);
                return false;
            }
        }

        message.ProcessedAt = _timeProvider.GetUtcNow();
        message.Attempts += 1;
        message.LastError = null;
        message.LockedUntil = null;
        return true;
    }

    private static string Truncate(string value) =>
        value.Length <= MaxErrorLength ? value : value[..MaxErrorLength];
}
