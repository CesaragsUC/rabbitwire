namespace MessageBroker.RabbitMq;

/// <summary>
/// Represents the configuration settings for a RabbitMQ endpoint, including queue name, routing key, exchange type, retry limits, and other properties that control the behavior of message consumption and production. This class is used to configure how messages are sent to or consumed from RabbitMQ queues, allowing for customization of message routing, retry policies, and prefetching behavior.
/// </summary>

public class RabbitWireEndpointConfig
{
    /// <summary>
    /// Gets or sets the name of the queue to which messages will be sent or from which messages will be consumed. This property is essential for routing messages to the correct destination in RabbitMQ. The queue name can be specified directly or constructed using a prefix defined in the RabbitWireConfig for better organization and namespacing of queues. 
    /// </summary>
    public string? QueueName { get; set; }

    /// <summary>
    /// Gets or sets the routing key used to determine the message destination.
    /// </summary>
    public string? RoutingKey { get; set; }

    /// <summary>
    /// Gets or sets the type of exchange to be used for message routing.
    /// </summary>
    public string? ExchangeType { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of retry attempts for a failed operation.
    /// </summary>
    /// <remarks>Set this property to control how many times an operation is retried before failing. A value
    /// of 0 disables retries.</remarks>
    public int RetryLimit { get; set; }

    /// <summary>
    /// Gets or sets the time interval between consecutive operations or events.
    /// </summary>
    public TimeSpan Interval { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the consume topology should be configured for the endpoint.
    /// </summary>
    /// <remarks>Set this property to <see langword="false"/> to prevent automatic configuration of the
    /// consume topology, which may be useful when customizing message routing or bindings.</remarks>
    public bool ConfigureConsumeTopology { get; set; }

    /// <summary>
    /// Gets or sets the number of messages to prefetch from the message queue.
    /// </summary>
    /// <remarks>Increasing the prefetch count can improve throughput by allowing more messages to be
    /// retrieved in advance, but may increase memory usage. Setting this value to zero or a negative number may disable
    /// prefetching, depending on the implementation.</remarks>
    public int PrefetchCount { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of items to prefetch in a single operation.
    /// </summary>
    public int PrefetchLimit { get; set; }
}