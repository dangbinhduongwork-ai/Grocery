using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using GroceryHub.SharedKernel;
using Microsoft.Extensions.DependencyInjection;

namespace GroceryHub.Persistence.Outbox;

public static class OutboxEventTypeRegistry
{
    public static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    private static readonly ConcurrentDictionary<string, Type> EventTypesByKey = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<Type, byte> RegisteredEventTypes = new();

    public static void Register<TEvent>()
        where TEvent : class, IIntegrationEvent =>
        Register(typeof(TEvent));

    public static void Register(Type eventType, string? explicitEventTypeName = null)
    {
        ArgumentNullException.ThrowIfNull(eventType);

        RegisteredEventTypes.TryAdd(eventType, 0);

        if (!string.IsNullOrWhiteSpace(explicitEventTypeName))
        {
            EventTypesByKey[explicitEventTypeName] = eventType;
        }

        if (!string.IsNullOrWhiteSpace(eventType.FullName))
        {
            EventTypesByKey[eventType.FullName] = eventType;
        }

        EventTypesByKey[eventType.Name] = eventType;

        if (!string.IsNullOrWhiteSpace(eventType.AssemblyQualifiedName))
        {
            EventTypesByKey[eventType.AssemblyQualifiedName] = eventType;
        }
    }

    public static Type? ResolveEventType(string eventTypeKey, string payload, IServiceProvider serviceProvider)
    {
        if (string.IsNullOrWhiteSpace(eventTypeKey))
        {
            return null;
        }

        if (EventTypesByKey.TryGetValue(eventTypeKey, out Type? cachedType))
        {
            return cachedType;
        }

        var candidateTypes = new HashSet<Type>(RegisteredEventTypes.Keys);

        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (assembly.IsDynamic)
            {
                continue;
            }

            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = ex.Types.Where(t => t is not null).Cast<Type>().ToArray();
            }

            foreach (Type type in types)
            {
                if (!type.IsAbstract && !type.IsInterface && typeof(IIntegrationEvent).IsAssignableFrom(type))
                {
                    candidateTypes.Add(type);

                    if (string.Equals(type.FullName, eventTypeKey, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(type.Name, eventTypeKey, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(type.AssemblyQualifiedName, eventTypeKey, StringComparison.OrdinalIgnoreCase))
                    {
                        Register(type, eventTypeKey);
                        return type;
                    }
                }
            }
        }

        foreach (Type candidate in candidateTypes)
        {
            try
            {
                if (JsonSerializer.Deserialize(payload, candidate, SerializerOptions) is IIntegrationEvent deserialized &&
                    string.Equals(deserialized.EventType, eventTypeKey, StringComparison.OrdinalIgnoreCase))
                {
                    Register(candidate, eventTypeKey);
                    return candidate;
                }
            }
            catch (JsonException)
            {
                // Ignore candidate that does not match the JSON payload shape.
            }
        }

        foreach (Type candidate in candidateTypes)
        {
            Type handlerServiceType = typeof(IIntegrationEventHandler<>).MakeGenericType(candidate);
            if (serviceProvider.GetServices(handlerServiceType).Any())
            {
                Register(candidate, eventTypeKey);
                return candidate;
            }
        }

        return null;
    }
}
