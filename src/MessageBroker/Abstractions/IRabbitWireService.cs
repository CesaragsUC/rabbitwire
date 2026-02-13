using MassTransit;

namespace MessageBroker.Abstractions;

/// <summary>
/// Defines the contract for a service that manages message bus interactions using RabbitMQ, including sending messages
/// and accessing the underlying bus control instance.
/// </summary>
/// <remarks>Implementations of this interface provide methods for sending messages to queues and accessing the
/// bus control for advanced messaging scenarios. This interface is intended for use in applications that require
/// integration with RabbitMQ via a managed message bus. Thread safety and lifecycle management depend on the specific
/// implementation.</remarks>
public interface IRabbitWireService
{
    /// <summary>
    /// Gets the unique identifier for this instance of the RabbitWireService. This can be used for logging, correlation, or other purposes where a unique instance ID is needed. 
    /// </summary>
    Guid InstanceId { get; }

    /// <summary>
    /// Gets the bus control instance used to manage and interact with the message bus.
    /// </summary>
    /// <remarks>Use this property to access the underlying bus for publishing, sending, or receiving
    /// messages. The returned instance provides methods for starting, stopping, and configuring the bus as
    /// needed.</remarks>
    IBusControl Bus { get; }

    /// <summary>
    /// Sends a message of the specified type to the given queue asynchronously.
    /// </summary>
    /// <typeparam name="T">The type of the message to send. Must be a reference type.</typeparam>
    /// <param name="message">The message instance to send. Cannot be null.</param>
    /// <param name="queueName">The URI identifying the target queue to which the message will be sent.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the send operation.</param>
    /// <returns>A task that represents the asynchronous send operation.</returns>
    Task Send<T>(T message, Uri queueName, CancellationToken cancellationToken = default)
    where T : class;

    /// <summary>
    /// Asynchronously sends the specified message.
    /// </summary>
    /// <typeparam name="T">The type of the message to send. Must be a reference type.</typeparam>
    /// <param name="message">The message instance to send. Cannot be null.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the send operation.</param>
    /// <returns>A task that represents the asynchronous send operation.</returns>
    Task Send<T>(T message, CancellationToken cancellationToken = default)
    where T : class;
}
