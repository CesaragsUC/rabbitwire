# 🐇 RabbitWire

[![NuGet](https://img.shields.io/nuget/v/RabbitWire.svg)](https://www.nuget.org/packages/RabbitWire)
[![License](https://img.shields.io/github/license/CesaragsUC/rabbitwire)](LICENSE)

**RabbitWire** is a lightweight .NET library that simplifies RabbitMQ integration using [MassTransit](https://masstransit.io/). It provides a convention-based setup for producers and consumers, automatic queue naming with environment prefixes, built-in retry policies, and a clean service abstraction — so you can focus on your business logic instead of messaging plumbing.

---

## ✨ Features

- **One-line setup** — Register RabbitMQ with a single `AddRabbitWire()` call.
- **Convention-based queue naming** — Queue names are automatically derived from consumer class names with a configurable prefix (e.g. `dev.productcreated.event.v1`).
- **Environment-aware prefixes** — Use prefixes like `dev`, `test`, `stage`, `prod` to isolate queues per environment.
- **Built-in retry & error handling** — Exponential backoff and interval-based retries are configured out of the box.
- **SSL/TLS support** — Optionally enable TLS 1.2 for secure connections.
- **Multiple consumers** — Pass as many consumer types as you need in a single registration call.
- **Clean send abstraction** — Use `IRabbitWireService` to send messages to any queue URI.

---

## 📦 Installation

```bash
dotnet add package RabbitWire
```

Or via the NuGet Package Manager:

```
Install-Package RabbitWire
```

---

## 🚀 Quick Start

### 1. Add Configuration

Add the following section to your `appsettings.json`:

```json
{
  "RabbitMqTransportOptions": {
    "Host": "localhost",
    "VHost": "/",
    "User": "guest",
    "Pass": "guest",
    "Port": 5672,
    "UseSsl": false,
    "Prefix": "dev"
  }
}
```

| Property   | Description                                                        |
|------------|--------------------------------------------------------------------|
| `Host`     | RabbitMQ server hostname.                                          |
| `VHost`    | Virtual host (default `/`).                                        |
| `User`     | RabbitMQ username.                                                 |
| `Pass`     | RabbitMQ password.                                                 |
| `Port`     | RabbitMQ port (default `5672`).                                    |
| `UseSsl`   | Set to `true` to enable TLS 1.2.                                   |
| `Prefix`   | Environment prefix for queue names (e.g. `dev`, `test`, `prod`).   |

### 2. Register RabbitWire in `Program.cs`

```csharp
builder.Services.AddRabbitWire(
    configuration,
    typeof(ProductCreatedConsumer),
    typeof(ProductUpdatedConsumer)
);
```

`AddRabbitWire` accepts **multiple consumer types** via `params`. Each consumer is automatically wired to a receive endpoint with a queue name derived from its class name and the configured prefix.

---

## 📖 Usage

### Sending Messages

RabbitWire provides `IRabbitWireService` for sending messages to RabbitMQ queues.

#### Define a Queue URI Provider (Recommended)

Create a small abstraction that maps your domain events to queue URIs. This keeps queue addresses centralized and easy to maintain.

**`IQueueUriProvider.cs`**

```csharp
public interface IQueueUriProvider
{
    Uri ProductCreatedMessage { get; }
    Uri ProductUpdatedMessage { get; }
    Uri ProductDeletedMessage { get; }
}
```

**`QueueUriProvider.cs`**

```csharp
using MessageBroker.RabbitMq;
using Microsoft.Extensions.Options;

public class QueueUriProvider : IQueueUriProvider
{
    private readonly RabbitWireConfig _rabbitMqOptions;

    public QueueUriProvider(IOptions<RabbitWireConfig> options)
    {
        _rabbitMqOptions = options.Value;
    }

    public Uri ProductCreatedMessage =>
        new Uri($"queue:{_rabbitMqOptions.Prefix}.mystore.productcreated.event.v1");

    public Uri ProductUpdatedMessage =>
        new Uri($"queue:{_rabbitMqOptions.Prefix}.mystore.productupdated.event.v1");

    public Uri ProductDeletedMessage =>
        new Uri($"queue:{_rabbitMqOptions.Prefix}.mystore.productdeleted.event.v1");
}
```

Register it in `Program.cs`:

```csharp
builder.Services.AddScoped<IQueueUriProvider, QueueUriProvider>();
```

#### Send a Message from a Handler

```csharp
public class ProductCreateHandler : IRequestHandler<CreateProductCommand, bool>
{
    private readonly IRabbitWireService _bus;
    private readonly IQueueUriProvider _queueUri;

    public ProductCreateHandler(IRabbitWireService bus, IQueueUriProvider queueUri)
    {
        _bus = bus;
        _queueUri = queueUri;
    }

    public async Task<bool> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var productCreatedEvent = new ProductCreatedEvent
        {
            Id = 1,
            Name = "Xbox",
            Price = 480.00m
        };

        await _bus.Send(productCreatedEvent, _queueUri.ProductCreatedMessage, cancellationToken);

        return true;
    }
}
```

### Consuming Messages

Create a consumer class that implements MassTransit's `IConsumer<T>`:

```csharp
public class ProductCreatedConsumer : IConsumer<ProductCreatedEvent>
{
    private readonly IMediator _mediator;

    public ProductCreatedConsumer(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task Consume(ConsumeContext<ProductCreatedEvent> context)
    {
        var product = new ProductAddedEvent
        {
            ProductId = context.Message.Id,
            Name = context.Message.Name,
            Price = context.Message.Price
        };

        await _mediator.Send(product);
    }
}
```

Then handle the domain event however you like — persist to a database, trigger notifications, etc.:

```csharp
public class ProductAddedEventHandler : IRequestHandler<ProductAddedEvent, bool>
{
    private readonly MyDbContext _context;
    private readonly IMapper _mapper;

    public ProductAddedEventHandler(MyDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<bool> Handle(ProductAddedEvent request, CancellationToken cancellationToken)
    {
        var product = _mapper.Map<Products>(request);

        _context.Products.Add(product);
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
```

---

## 🔧 How It Works

### Queue Naming Convention

RabbitWire automatically derives queue names from consumer class names using the following convention:

```
{Prefix}.{consumerName without "Consumer" suffix}.event.v1
```

**Examples:**

| Consumer Class              | Prefix  | Generated Queue Name                     |
|-----------------------------|---------|------------------------------------------|
| `ProductCreatedConsumer`    | `dev`   | `dev.productcreated.event.v1`            |
| `ProductUpdatedConsumer`    | `prod`  | `prod.productupdated.event.v1`           |
| `OrderShippedConsumer`      | `test`  | `test.ordershipped.event.v1`             |

### Built-in Retry Policy

Each consumer endpoint is configured with:

- **Interval retry**: 3 attempts with a 3-second delay between each.
- **Exponential backoff**: 3 attempts starting at 5 seconds, up to 1 hour max, with a 10-second increment.
- **Error logging** via [Serilog](https://serilog.net/) on each retry attempt.
- `ConsumerCanceledException` is explicitly ignored by the retry policy.

### Endpoint Defaults

| Setting                   | Default Value |
|---------------------------|---------------|
| `PrefetchCount`           | `5`           |
| `ConfigureConsumeTopology`| `false`       |
| `AutoDelete`              | `false`       |
| `ExchangeType`            | `Fanout`      |

---

## 🏗️ Architecture Overview

```
┌─────────────────────────────────────────────────────────────┐
│  Your Application                                           │
│                                                             │
│  ┌──────────────┐    ┌──────────────────┐                   │
│  │   Handler    │───▶│ IRabbitWireService│──── Send ───▶ 🐇 │
│  └──────────────┘    └──────────────────┘           RabbitMQ│
│                                                             │
│  ┌──────────────────────────┐                               │
│  │ ProductCreatedConsumer   │◀── Consume ──────────── 🐇    │
│  │ ProductUpdatedConsumer   │                        RabbitMQ│
│  └──────────────────────────┘                               │
│                                                             │
│  Program.cs                                                 │
│  ┌──────────────────────────────────────────────────┐       │
│  │ builder.Services.AddRabbitWire(config, consumers)│       │
│  └──────────────────────────────────────────────────┘       │
└─────────────────────────────────────────────────────────────┘
```

---

## 📚 API Reference

### `AddRabbitWire`

```csharp
public static IServiceCollection AddRabbitWire(
    this IServiceCollection services,
    IConfiguration configuration,
    params Type[] consumerAssemblies)
```

Registers MassTransit with RabbitMQ transport and configures all provided consumer types. Reads connection settings from the `RabbitMqTransportOptions` configuration section.

### `IRabbitWireService`

| Method | Description |
|--------|-------------|
| `Send<T>(T message, Uri queueName, CancellationToken ct)` | Sends a message to a specific queue URI. |
| `Send<T>(T message, CancellationToken ct)` | Sends a message using MassTransit endpoint conventions. |

| Property     | Description                              |
|--------------|------------------------------------------|
| `InstanceId` | Unique identifier for this service instance. |
| `Bus`        | The underlying MassTransit `IBusControl`.    |

### `RabbitWireConfig`

Extends MassTransit's `RabbitMqTransportOptions` with:

| Property | Type      | Description                                |
|----------|-----------|--------------------------------------------|
| `Prefix` | `string?` | Environment prefix appended to queue names. |

---

## 🔒 SSL / TLS

To enable secure connections, set `UseSsl` to `true` in your configuration:

```json
{
  "RabbitMqTransportOptions": {
    "Host": "rabbitmq.example.com",
    "UseSsl": true
  }
}
```

RabbitWire will use **TLS 1.2** for the connection.

---

## 📋 Requirements

- **.NET 10** or later
- **RabbitMQ** server (local or remote)
- **MassTransit** 9.x (included as a dependency)

---

## 🤝 Contributing

Contributions are welcome! Feel free to open issues or submit pull requests on [GitHub](https://github.com/CesaragsUC/rabbitwire).

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/my-feature`)
3. Commit your changes (`git commit -m 'Add my feature'`)
4. Push to the branch (`git push origin feature/my-feature`)
5. Open a Pull Request

---

## 📄 License

This project is licensed under the terms of the license included in the repository. See the [LICENSE](LICENSE) file for details.

---

Made with ❤️ by [Casoft](https://github.com/CesaragsUC)