namespace Shared.Exceptions;

// Service Communication
public class ServiceCommunicationException : Exception
{
    public string ServiceName { get; }
    public string Endpoint { get; }

    public ServiceCommunicationException(string serviceName, string endpoint, string message)
        : base($"Communication with {serviceName} failed at {endpoint}: {message}")
    {
        ServiceName = serviceName;
        Endpoint = endpoint;
    }

    public ServiceCommunicationException(string serviceName, string endpoint, string message, Exception inner)
        : base($"Communication with {serviceName} failed at {endpoint}: {message}", inner)
    {
        ServiceName = serviceName;
        Endpoint = endpoint;
    }
}

public class ServiceUnavailableException : Exception
{
    public string ServiceName { get; }
    public ServiceUnavailableException(string serviceName)
        : base($"Service '{serviceName}' is currently unavailable")
    {
        ServiceName = serviceName;
    }
}

public class CircuitBreakerOpenException : Exception
{
    public string ServiceName { get; }
    public CircuitBreakerOpenException(string serviceName)
        : base($"Circuit breaker is open for service '{serviceName}'. Service temporarily unavailable.")
    {
        ServiceName = serviceName;
    }
}

// Database
public class DataAccessException : Exception
{
    public string Operation { get; }
    public string EntityType { get; }

    public DataAccessException(string operation, string entityType, string message)
        : base($"Data access error during {operation} on {entityType}: {message}")
    {
        Operation = operation;
        EntityType = entityType;
    }

    public DataAccessException(string operation, string entityType, string message, Exception inner)
        : base($"Data access error during {operation} on {entityType}: {message}", inner)
    {
        Operation = operation;
        EntityType = entityType;
    }
}

public class ConcurrencyConflictException : Exception
{
    public string EntityType { get; }
    public int EntityId { get; }

    public ConcurrencyConflictException(string entityType, int entityId)
        : base($"Concurrency conflict detected for {entityType} (ID: {entityId}). The record was modified by another user.")
    {
        EntityType = entityType;
        EntityId = entityId;
    }
}

// Cache
public class CacheOperationException : Exception
{
    public string CacheKey { get; }
    public CacheOperationException(string cacheKey, string message)
        : base($"Cache operation failed for key '{cacheKey}': {message}")
    {
        CacheKey = cacheKey;
    }
}

// Message Queue
public class MessagePublishException : Exception
{
    public string EventType { get; }
    public string QueueName { get; }

    public MessagePublishException(string eventType, string queueName, string message)
        : base($"Failed to publish event '{eventType}' to queue '{queueName}': {message}")
    {
        EventType = eventType;
        QueueName = queueName;
    }

    public MessagePublishException(string eventType, string queueName, string message, Exception inner)
        : base($"Failed to publish event '{eventType}' to queue '{queueName}': {message}", inner)
    {
        EventType = eventType;
        QueueName = queueName;
    }
}

public class MessageConsumptionException : Exception
{
    public string EventType { get; }
    public int RetryCount { get; }

    public MessageConsumptionException(string eventType, int retryCount, string message)
        : base($"Failed to consume event '{eventType}' after {retryCount} retries: {message}")
    {
        EventType = eventType;
        RetryCount = retryCount;
    }
}

// Saga
public class SagaCompensationFailedException : Exception
{
    public string SagaId { get; }
    public string FailedStep { get; }
    public int RetryCount { get; }

    public SagaCompensationFailedException(string sagaId, string failedStep, int retryCount)
        : base($"Saga {sagaId} compensation failed at step '{failedStep}' after {retryCount} retries")
    {
        SagaId = sagaId;
        FailedStep = failedStep;
        RetryCount = retryCount;
    }
}

public class SagaTimeoutException : Exception
{
    public string SagaId { get; }
    public string PendingStep { get; }

    public SagaTimeoutException(string sagaId, string pendingStep)
        : base($"Saga {sagaId} timed out at step '{pendingStep}'")
    {
        SagaId = sagaId;
        PendingStep = pendingStep;
    }
}

// Configuration
public class ConfigurationException : Exception
{
    public string ConfigurationKey { get; }
    public ConfigurationException(string key, string message)
        : base($"Configuration error for '{key}': {message}")
    {
        ConfigurationKey = key;
    }
}
