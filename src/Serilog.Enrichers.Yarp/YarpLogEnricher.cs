using Microsoft.AspNetCore.Http;
using Serilog.Core;
using Serilog.Events;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;

namespace Serilog.Enrichers.Yarp
{
    /// <summary>
    /// Serilog enricher that adds YARP reverse proxy context to log events.
    /// This enricher extracts RouteId, ClusterId, and DestinationId from HttpContext
    /// without taking a hard dependency on YARP packages.
    /// </summary>
    public class YarpLogEnricher : ILogEventEnricher
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly YarpEnricherOptions _options;
        
        // Cache for PropertyInfo lookups to improve performance
        private static readonly ConcurrentDictionary<(Type, string), PropertyInfo?> _propertyCache = 
            new ConcurrentDictionary<(Type, string), PropertyInfo?>();

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

            // Try to extract YARP context using reflection to avoid hard dependencies
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
        /// Extracts YARP context from HttpContext using reflection to avoid hard dependencies on YARP packages.
        /// This method safely handles cases where YARP is not present.
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
                // YARP stores proxy information in HttpContext.Features
                // We use reflection to access it without taking a dependency

                // Try to get the feature from HttpContext.Features collection
                var features = httpContext.Features;
                if (features == null)
                {
                    return (null, null, null);
                }

                // Look for YARP feature by checking features that match YARP patterns
                var yarpFeatures = features
                    .Select(kvp => kvp.Value)
                    .Where(feature => feature != null)
                    .Where(feature =>
                    {
                        var featureTypeName = feature.GetType().FullName ?? feature.GetType().Name;
                        return featureTypeName.Contains("ReverseProxy", StringComparison.OrdinalIgnoreCase) ||
                               featureTypeName.Contains("Yarp", StringComparison.OrdinalIgnoreCase);
                    });

                foreach (var feature in yarpFeatures)
                {
                    // Try to extract RouteId
                    if (routeId == null)
                    {
                        routeId = TryGetPropertyValue(feature, "RouteId") ??
                                 TryGetPropertyValue(feature, "Route")?.ToString();
                    }

                    // Try to extract ClusterId
                    if (clusterId == null)
                    {
                        clusterId = TryGetPropertyValue(feature, "ClusterId") ??
                                   TryGetPropertyValue(feature, "Cluster")?.ToString();
                    }

                    // Try to extract DestinationId
                    if (destinationId == null)
                    {
                        destinationId = TryGetPropertyValue(feature, "DestinationId") ??
                                       TryGetPropertyValue(feature, "Destination")?.ToString();
                    }
                }

                // Also check HttpContext.Items for YARP context (alternative storage location)
                if (httpContext.Items != null)
                {
                    routeId ??= httpContext.Items["YarpRouteId"]?.ToString() ??
                               httpContext.Items["Yarp.RouteId"]?.ToString();
                    clusterId ??= httpContext.Items["YarpClusterId"]?.ToString() ??
                                 httpContext.Items["Yarp.ClusterId"]?.ToString();
                    destinationId ??= httpContext.Items["YarpDestinationId"]?.ToString() ??
                                     httpContext.Items["Yarp.DestinationId"]?.ToString();
                }
            }
            catch (Exception ex)
            {
                // Silently fail if reflection fails - we don't want to crash the application
                // This is expected if YARP is not present or the structure changes
#if DEBUG
                System.Diagnostics.Debug.WriteLine(
                    $"Failed to extract YARP context from HttpContext: {ex.Message}");
#endif
            }

            return (routeId, clusterId, destinationId);
        }

        /// <summary>
        /// Tries to get a property value from an object using reflection with caching for performance.
        /// </summary>
        /// <param name="obj">The object to get the property from.</param>
        /// <param name="propertyName">The name of the property.</param>
        /// <returns>The property value as a string, or null if not found.</returns>
        private static string? TryGetPropertyValue(object obj, string propertyName)
        {
            try
            {
                var type = obj.GetType();
                var cacheKey = (type, propertyName);
                
                // Use cached PropertyInfo if available
                var property = _propertyCache.GetOrAdd(cacheKey, key =>
                {
                    return key.Item1.GetProperty(key.Item2, 
                        BindingFlags.Public | 
                        BindingFlags.Instance | 
                        BindingFlags.IgnoreCase);
                });

                if (property != null)
                {
                    var value = property.GetValue(obj);
                    return value?.ToString();
                }
            }
            catch (Exception ex)
            {
                // Optionally log for diagnostics - can be enabled via conditional compilation
#if DEBUG
                System.Diagnostics.Debug.WriteLine(
                    $"Failed to get property '{propertyName}' from type '{obj.GetType().Name}': {ex.Message}");
#endif
            }

            return null;
        }
    }
}
