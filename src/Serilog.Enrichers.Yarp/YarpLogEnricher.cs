using Microsoft.AspNetCore.Http;
using Serilog.Core;
using Serilog.Events;
using System;
using Yarp.ReverseProxy.Model;

namespace Serilog.Enrichers.Yarp
{
    /// <summary>
    /// Serilog enricher that adds YARP reverse proxy context to log events.
    /// This enricher extracts RouteId, ClusterId, and DestinationId from HttpContext
    /// using direct integration with YARP.ReverseProxy package.
    /// </summary>
    public class YarpLogEnricher : ILogEventEnricher
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly YarpEnricherOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="YarpLogEnricher"/> class.
        /// </summary>
        /// <param name="httpContextAccessor">The HTTP context accessor to retrieve the current request context.</param>
        /// <param name="options">Configuration options for the enricher. If null, default options are used.</param>
        /// <exception cref="ArgumentNullException">Thrown when httpContextAccessor is null.</exception>
        public YarpLogEnricher(IHttpContextAccessor httpContextAccessor, YarpEnricherOptions? options = null)
        {
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _options = options ?? new YarpEnricherOptions();
            _options.Validate();
        }

        /// <summary>
        /// Enriches the log event with YARP proxy context properties.
        /// </summary>
        /// <param name="logEvent">The log event to enrich.</param>
        /// <param name="propertyFactory">Factory for creating log event properties.</param>
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            if (logEvent == null) throw new ArgumentNullException(nameof(logEvent));
            if (propertyFactory == null) throw new ArgumentNullException(nameof(propertyFactory));

            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
            {
                return;
            }

            // Extract YARP context using YARP's public API
            var yarpContext = ExtractYarpContext(httpContext);

            if (yarpContext.RouteId != null && _options.IncludeRouteId)
            {
                var property = propertyFactory.CreateProperty(_options.RouteIdPropertyName, yarpContext.RouteId);
                logEvent.AddPropertyIfAbsent(property);
            }

            if (yarpContext.ClusterId != null && _options.IncludeClusterId)
            {
                var property = propertyFactory.CreateProperty(_options.ClusterIdPropertyName, yarpContext.ClusterId);
                logEvent.AddPropertyIfAbsent(property);
            }

            if (yarpContext.DestinationId != null && _options.IncludeDestinationId)
            {
                var property = propertyFactory.CreateProperty(_options.DestinationIdPropertyName, yarpContext.DestinationId);
                logEvent.AddPropertyIfAbsent(property);
            }

            // Add correlation context if enabled
            if (_options.IncludeCorrelationContext && httpContext.TraceIdentifier != null)
            {
                var traceProperty = propertyFactory.CreateProperty("TraceIdentifier", httpContext.TraceIdentifier);
                logEvent.AddPropertyIfAbsent(traceProperty);
            }
        }

        /// <summary>
        /// Extracts YARP context from HttpContext using YARP's IReverseProxyFeature.
        /// </summary>
        /// <param name="httpContext">The HTTP context.</param>
        /// <returns>A tuple containing RouteId, ClusterId, and DestinationId if available.</returns>
        private static (string? RouteId, string? ClusterId, string? DestinationId) ExtractYarpContext(HttpContext httpContext)
        {
            string? routeId = null;
            string? clusterId = null;
            string? destinationId = null;

            try
            {
                // Get YARP's IReverseProxyFeature from the HttpContext features
                var reverseProxyFeature = httpContext.Features.Get<IReverseProxyFeature>();
                
                if (reverseProxyFeature != null)
                {
                    // Extract RouteId from the Route
                    if (reverseProxyFeature.Route != null)
                    {
                        routeId = reverseProxyFeature.Route.Config?.RouteId;
                    }

                    // Extract ClusterId from the Cluster
                    if (reverseProxyFeature.Cluster != null)
                    {
                        clusterId = reverseProxyFeature.Cluster.Config?.ClusterId;
                    }

                    // Extract DestinationId from the ProxiedDestination
                    if (reverseProxyFeature.ProxiedDestination != null)
                    {
                        destinationId = reverseProxyFeature.ProxiedDestination.DestinationId;
                    }
                }
            }
            catch (Exception ex)
            {
                // Silently fail if extraction fails - we don't want to crash the application
                // This is expected if YARP feature is not available
#if DEBUG
                System.Diagnostics.Debug.WriteLine(
                    $"Failed to extract YARP context from HttpContext: {ex.Message}");
#endif
            }

            return (routeId, clusterId, destinationId);
        }
    }
}
