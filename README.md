# Serilog.Enrichers.Yarp

[![NuGet](https://img.shields.io/nuget/v/Serilog.Enrichers.Yarp.svg)](https://www.nuget.org/packages/Serilog.Enrichers.Yarp)
[![License](https://img.shields.io/github/license/brandonhenricks/serilog-enrichers-yarp)](LICENSE)

A production-grade Serilog enricher that adds YARP (Yet Another Reverse Proxy) reverse proxy context to log events in ASP.NET Core applications. This library enriches logs with critical proxy information (RouteId, ClusterId, DestinationId) extracted from HttpContext using **direct integration with YARP.ReverseProxy package**.

## Features

- ✅ **Direct YARP Integration**: Uses YARP's public APIs for type-safe context extraction
- ✅ **Production-Ready**: Follows SOLID, DRY, and KISS principles
- ✅ **Configurable**: Customize property names and control what gets logged
- ✅ **DI-Friendly**: Easy integration with ASP.NET Core dependency injection
- ✅ **Distributed Tracing**: Supports correlation context for distributed systems
- ✅ **Safe Low-Cardinality**: Enriches with safe, low-cardinality fields suitable for production logging
- ✅ **.NET 6.0+**: Compatible with .NET 6.0 and higher
- ✅ **Well-Tested**: Comprehensive unit test coverage (39 tests)

## Installation

Install via NuGet:

```bash
dotnet add package Serilog.Enrichers.Yarp
```

Or via Package Manager:

```powershell
Install-Package Serilog.Enrichers.Yarp
```

## Quick Start

### 1. Register Services (Recommended)

In your `Program.cs` or `Startup.cs`:

```csharp
using Serilog;
using Serilog.Enrichers.Yarp;

var builder = WebApplication.CreateBuilder(args);

// Register YARP enricher services (registers IHttpContextAccessor)
builder.Services.AddYarpLogEnricher();

// Configure Serilog with YARP enrichment
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.WithYarpContext(services.GetRequiredService<IHttpContextAccessor>())
    .WriteTo.Console());

var app = builder.Build();
app.Run();
```

### 2. Direct Configuration

If you prefer not to use DI:

```csharp
using Serilog;
using Serilog.Enrichers.Yarp;
using Microsoft.AspNetCore.Http;

var httpContextAccessor = new HttpContextAccessor();

Log.Logger = new LoggerConfiguration()
    .Enrich.WithYarpContext(httpContextAccessor)
    .WriteTo.Console()
    .CreateLogger();
```

## Configuration Options

### Default Configuration

By default, the enricher includes all available YARP context properties:

```csharp
services.AddYarpLogEnricher();

builder.Host.UseSerilog((context, services, configuration) => configuration
    .Enrich.WithYarpContext(services.GetRequiredService<IHttpContextAccessor>())
    .WriteTo.Console());
```

This will add the following properties to your log events (when available):
- `YarpRouteId` - The YARP route identifier
- `YarpClusterId` - The YARP cluster identifier
- `YarpDestinationId` - The YARP destination identifier
- `TraceIdentifier` - ASP.NET Core trace identifier for correlation

### Custom Configuration

Customize which properties are logged and their names:

```csharp
services.AddYarpLogEnricher(options =>
{
    options.IncludeRouteId = true;
    options.IncludeClusterId = true;
    options.IncludeDestinationId = true;
    options.IncludeCorrelationContext = true;
    
    // Customize property names
    options.RouteIdPropertyName = "ProxyRoute";
    options.ClusterIdPropertyName = "ProxyCluster";
    options.DestinationIdPropertyName = "ProxyDestination";
});

builder.Host.UseSerilog((context, services, configuration) => configuration
    .Enrich.WithYarpContext(
        services.GetRequiredService<IHttpContextAccessor>(),
        services.GetRequiredService<YarpEnricherOptions>())
    .WriteTo.Console());
```

### Inline Configuration

Configure options inline without DI:

```csharp
builder.Host.UseSerilog((context, services, configuration) => configuration
    .Enrich.WithYarpContext(
        services.GetRequiredService<IHttpContextAccessor>(),
        opts =>
        {
            opts.IncludeRouteId = true;
            opts.IncludeClusterId = false; // Don't log cluster
            opts.IncludeDestinationId = true;
        })
    .WriteTo.Console());
```

## How It Works

The enricher uses YARP's public `IReverseProxyFeature` API to extract proxy context from `HttpContext`. This approach:

1. **Uses IReverseProxyFeature**: Accesses YARP's feature from HttpContext.Features
2. **Type-Safe Extraction**: Uses YARP's RouteModel, ClusterModel, and DestinationState types directly
3. **Safe Failure**: Silently handles cases where YARP feature is not present

This design ensures:
- ✅ Type-safe integration with YARP
- ✅ Works with YARP 2.2.0 and later versions
- ✅ Safe for non-YARP requests (no errors if YARP isn't processing the request)
- ✅ Zero performance impact when YARP context is not available

## Log Output Example

When YARP context is available, your logs will include:

```json
{
  "Timestamp": "2026-01-27T10:15:30.1234567Z",
  "Level": "Information",
  "MessageTemplate": "Request processed successfully",
  "Properties": {
    "YarpRouteId": "api-route",
    "YarpClusterId": "backend-cluster",
    "YarpDestinationId": "backend-server-1",
    "TraceIdentifier": "0HMVFE3QK9K8H:00000001"
  }
}
```

## Advanced Scenarios

### Structured Logging with Elastic/Seq

```csharp
builder.Host.UseSerilog((context, services, configuration) => configuration
    .Enrich.WithYarpContext(services.GetRequiredService<IHttpContextAccessor>())
    .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri("http://localhost:9200"))
    {
        AutoRegisterTemplate = true,
        IndexFormat = "logs-{0:yyyy.MM}"
    }));
```

### Conditional Enrichment

```csharp
services.AddYarpLogEnricher(options =>
{
    // Only log route and cluster in production
    var isProduction = builder.Environment.IsProduction();
    options.IncludeRouteId = isProduction;
    options.IncludeClusterId = isProduction;
    options.IncludeDestinationId = true; // Always log destination
});
```

### Multiple Enrichers

```csharp
builder.Host.UseSerilog((context, services, configuration) => configuration
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithEnvironmentName()
    .Enrich.WithYarpContext(services.GetRequiredService<IHttpContextAccessor>())
    .WriteTo.Console());
```

## Best Practices

1. **Use DI Registration**: Always call `AddYarpLogEnricher()` to ensure `IHttpContextAccessor` is registered
2. **Configure Early**: Set up Serilog enrichment during application startup
3. **Low Cardinality**: The enricher only logs safe, low-cardinality fields suitable for production
4. **Correlation**: Enable `IncludeCorrelationContext` for distributed tracing scenarios
5. **Customize Names**: Use custom property names to match your logging conventions

## Requirements

- .NET 6.0 or higher
- Serilog 2.10.0 or higher
- ASP.NET Core 6.0 or higher (for HttpContext support)
- **YARP.ReverseProxy 2.2.0 or higher** (automatically included as a dependency)

## Migration from v1.x to v2.0

Version 2.0 introduces **breaking changes** due to the direct YARP integration:

### What Changed
- **Target Framework**: Changed from `netstandard2.0` to `net6.0`
- **YARP Dependency**: Now requires `Yarp.ReverseProxy` package (version 2.2.0+) as a direct dependency
- **Implementation**: Replaced reflection-based extraction with YARP's public `IReverseProxyFeature` API

### Migration Steps
1. **Update Target Framework**: Ensure your application targets .NET 6.0 or higher
2. **Update Package**: Update to version 2.0.0 or higher:
   ```bash
   dotnet add package Serilog.Enrichers.Yarp --version 2.0.0
   ```
3. **No Code Changes Required**: The public API remains the same; only the internal implementation changed
4. **Verify YARP Version**: If you're already using YARP, ensure you're on version 2.2.0 or later

### Benefits of Migration
- Type-safe integration with YARP
- Better long-term maintainability
- No reliance on reflection or internal YARP APIs
- Improved performance through direct API access

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- Built with [Serilog](https://serilog.net/)
- Designed for use with [YARP (Yet Another Reverse Proxy)](https://microsoft.github.io/reverse-proxy/)

## Support

For issues, questions, or contributions, please visit the [GitHub repository](https://github.com/brandonhenricks/serilog-enrichers-yarp).
