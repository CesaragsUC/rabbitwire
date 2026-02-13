using MassTransit;
using MessageBroker.Abstractions;
using MessageBroker.RabbitMq;
using MessageBroker.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System.Security.Authentication;

namespace MMessageBroker.Configurations;


public static class RabbitWireServiceExtensions
{

    /// <summary>
    /// Add and configure MassTransit with RabbitMQ transport using settings from configuration.
    /// </summary>
    /// <param name="services">The service collection to add the MassTransit configuration to.</param>
    /// <param name="configuration">The configuration containing RabbitMQ settings.</param>
    /// <param name="consumerAssemblies">Assemblies containing MassTransit consumers.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddRabbitWire(
        this IServiceCollection services,
        IConfiguration configuration,
        params Type[] consumerAssemblies)
    {
        services.Configure<RabbitWireConfig>(configuration.GetSection("RabbitMqTransportOptions"));

        var rabbitMqOptions = new RabbitWireConfig();
        configuration.GetSection("RabbitMqTransportOptions").Bind(rabbitMqOptions);

        services.AddSingleton<IRabbitWireService, RabbitWireService>();
        services.AddMassTransit(x =>
        {
            foreach (var assembly in consumerAssemblies)
            {
                x.AddConsumers(assembly.Assembly);
            }

            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(rabbitMqOptions.Host, host =>
                {
                    host.Username(rabbitMqOptions.User);
                    host.Password(rabbitMqOptions.Pass);

                    if (rabbitMqOptions.UseSsl)
                        host.UseSsl(ssl => ssl.Protocol = SslProtocols.Tls12);
                });

                foreach (var consumerType in consumerAssemblies)
                {
                    var queueConfig = BuildEndpointConfig(consumerType, rabbitMqOptions);

                    var method = typeof(RabbitWireServiceExtensions)
                        .GetMethod(nameof(ConfigureEndpoint),
                        System.Reflection.BindingFlags.NonPublic |
                        System.Reflection.BindingFlags.Static)
                        ?.MakeGenericMethod(consumerType);

                    method?.Invoke(null, new object[] { cfg, context, queueConfig });
                }
            });
        });

        return services;
    }


    /// <summary>
    /// Configures a RabbitMQ receive endpoint for the specified consumer type using the provided endpoint settings and
    /// registration context.
    /// </summary>
    /// <remarks>This method sets up message retry policies, prefetch count, and consumer topology for the
    /// endpoint. It is intended to be used as an extension method to streamline endpoint configuration for consumers in
    /// MassTransit-based applications.</remarks>
    /// <typeparam name="TConsumer">The consumer type to be configured for the receive endpoint. Must implement the IConsumer interface.</typeparam>
    /// <param name="configRabbit">The RabbitMQ bus factory configurator used to define the receive endpoint.</param>
    /// <param name="context">The bus registration context that provides access to dependency injection and consumer configuration.</param>
    /// <param name="endpointConfig">The configuration settings for the RabbitMQ endpoint, including queue name, prefetch count, retry policy, and
    /// topology options.</param>
    private static void ConfigureEndpoint<TConsumer>(
    this IRabbitMqBusFactoryConfigurator configRabbit,
    IBusRegistrationContext context,
    RabbitWireEndpointConfig endpointConfig)
    where TConsumer : class, IConsumer
    {
        configRabbit.ReceiveEndpoint(endpointConfig.QueueName!, configureEndpoint =>
        {
            configureEndpoint.ConfigureConsumeTopology = endpointConfig.ConfigureConsumeTopology;
            configureEndpoint.PrefetchCount = endpointConfig.PrefetchCount;
            configureEndpoint.UseMessageRetry(retry =>
            {
                retry.Interval(endpointConfig.RetryLimit, endpointConfig.Interval);
                retry.Ignore<ConsumerCanceledException>();
                retry.Exponential(3, TimeSpan.FromSeconds(5), TimeSpan.FromHours(1), TimeSpan.FromSeconds(10))
                    .Handle<Exception>();

                retry.Handle<Exception>(ex =>
                {
                    Log.Error(ex, $"An Error occour on retry: {ex.Message}");
                    return  true;
                });
            });

            configureEndpoint.AutoDelete  = false;
            configureEndpoint.ConfigureConsumer<TConsumer>(context);
        });
    }

    /// <summary>
    /// Creates a new RabbitMQ endpoint configuration for the specified consumer type using the provided RabbitMQ
    /// settings.
    /// </summary>
    /// <remarks>The generated queue name and routing key are derived from the consumer type name. The method
    /// applies a standard naming convention and sets default values for exchange type, retry limit, interval, and
    /// prefetch count.</remarks>
    /// <param name="consumerType">The type of the consumer for which to generate the endpoint configuration. The type name is used to determine
    /// the event name and routing key.</param>
    /// <param name="rabbitMqConfig">The RabbitMQ configuration settings to use when constructing the endpoint configuration. Must not be null.</param>
    /// <returns>A configured instance of RabbitMqEndpointConfig representing the endpoint settings for the specified consumer
    /// type.</returns>
    private static RabbitWireEndpointConfig BuildEndpointConfig(Type consumerType, RabbitWireConfig rabbitMqConfig)
    {
        var eventName = consumerType.Name.Replace("Consumer", ".event");

        return new RabbitWireEndpointConfig
        {
            QueueName = $"{rabbitMqConfig.Prefix}.{eventName.ToLower()}.v1",
            RoutingKey = eventName,
            ExchangeType = RabbitMQ.Client.ExchangeType.Fanout,
            RetryLimit = 3,
            Interval = TimeSpan.FromSeconds(3),
            ConfigureConsumeTopology = false,
            PrefetchCount = 5
        };
    }
}
