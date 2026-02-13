using MassTransit;

namespace MessageBroker.Abstractions;

public interface IRabbitWireService
{
    Guid InstanceId { get; }

    IBusControl Bus { get; }

    Task Send<T>(T message, Uri queueName, CancellationToken cancellationToken = default)
    where T : class;

    Task Send<T>(T message, CancellationToken cancellationToken = default)
    where T : class;
}
