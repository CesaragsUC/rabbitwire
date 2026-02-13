using MassTransit;

namespace MessageBroker.RabbitMq;

/// <summary>
/// Is a class to extend the RabbitMqTransportOptions
/// Here we can add more properties to the RabbitMqTransportOptions
/// </summary>
public class RabbitWireConfig : RabbitMqTransportOptions
{
    /// <summary>
    /// Gets or sets the prefix to be added to the queue names. This can be useful for namespacing queues in a multi-tenant environment or when sharing a RabbitMQ instance among multiple applications.
    /// </summary>
    /// <param name="Prefix"> The prefix to be added to the queue names. This can be useful for namespacing queues in a multi-tenant environment or when sharing a RabbitMQ instance among multiple applications.</param>
    public string? Prefix { get; set; }
}
