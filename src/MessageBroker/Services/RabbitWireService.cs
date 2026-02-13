using MassTransit;
using MessageBroker.Abstractions;
using MessageBroker.RabbitMq;
using Microsoft.Extensions.Options;
using System.Security.Authentication;

namespace MessageBroker.Services;

/// <summary>
/// Implements the IRabbitWireService interface to provide functionality for sending messages to RabbitMQ queues using MassTransit. This service manages the bus control instance and allows for sending messages to specific queues or using conventions to determine the destination address. The service is designed to be used in applications that require integration with RabbitMQ for message-based communication, providing a simple API for sending messages while abstracting away the underlying bus configuration and management details. 
/// </summary>
public class RabbitWireService : IRabbitWireService
{
    public Guid InstanceId { get; }
    public IBusControl Bus { get; }

    public RabbitWireService(IOptions<RabbitWireConfig> options,
        Action<IRabbitMqBusFactoryConfigurator>? extraConfig = null)
    {
        InstanceId = Guid.NewGuid();

        var config = options.Value;
        Bus = MassTransit.Bus.Factory.CreateUsingRabbitMq(cfg =>
        {
            cfg.Host(config.Host, hostConfigurator =>
            {
                hostConfigurator.Username(config.User);
                hostConfigurator.Password(config.Pass);

                if (options.Value.UseSsl)
                    hostConfigurator.UseSsl(ssl => ssl.Protocol = SslProtocols.Tls12);
            });

            extraConfig?.Invoke(cfg);
        });
    }

    public async Task Send<T>(T message, Uri queueName, CancellationToken cancellationToken = default) where T : class
    {
        var endpoint = await Bus.GetSendEndpoint(queueName).ConfigureAwait(false);
        await endpoint.Send(message, cancellationToken).ConfigureAwait(false);
    }

    public async Task Send<T>(T message, CancellationToken cancellationToken = default)
    where T : class
    {
        if (!EndpointConvention.TryGetDestinationAddress<T>(out var destinationAddress))
            throw new ArgumentException($"A convention for the message type {TypeCache<T>.ShortName} was not found");

        var endpoint = await Bus.GetSendEndpoint(destinationAddress).ConfigureAwait(false);

        await endpoint.Send(message, cancellationToken).ConfigureAwait(false);
    }
}