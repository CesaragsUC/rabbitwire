using MassTransit;

namespace MessageBroker.RabbitMq;

/// <summary>
/// Is a class to extend the RabbitMqTransportOptions
/// Here we can add more properties to the RabbitMqTransportOptions
/// </summary>
public class RabbitWireConfig : RabbitMqTransportOptions
{
    public string? Prefix { get; set; }
}
