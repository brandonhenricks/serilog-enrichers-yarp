using System;

namespace Serilog.Enrichers.Yarp
{
    /// <summary>
    /// Configuration options for the YARP log enricher.
    /// Warning: Options should not be modified after being passed to the enricher or validated,
    /// as validation is only performed once during enricher construction.
    /// </summary>
    public sealed class YarpEnricherOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether to include the RouteId in log events.
        /// Default is true.
        /// </summary>
        public bool IncludeRouteId { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to include the ClusterId in log events.
        /// Default is true.
        /// </summary>
        public bool IncludeClusterId { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to include the DestinationId in log events.
        /// Default is true.
        /// </summary>
        public bool IncludeDestinationId { get; set; } = true;

        /// <summary>
        /// Gets or sets the property name for the RouteId.
        /// Default is "YarpRouteId".
        /// </summary>
        public string RouteIdPropertyName { get; set; } = "YarpRouteId";

        /// <summary>
        /// Gets or sets the property name for the ClusterId.
        /// Default is "YarpClusterId".
        /// </summary>
        public string ClusterIdPropertyName { get; set; } = "YarpClusterId";

        /// <summary>
        /// Gets or sets the property name for the DestinationId.
        /// Default is "YarpDestinationId".
        /// </summary>
        public string DestinationIdPropertyName { get; set; } = "YarpDestinationId";

        /// <summary>
        /// Gets or sets a value indicating whether to include correlation context for distributed tracing.
        /// When enabled, includes the ASP.NET Core TraceIdentifier for request correlation.
        /// Default is true.
        /// </summary>
        public bool IncludeCorrelationContext { get; set; } = true;

        /// <summary>
        /// Validates the options and throws an ArgumentException if invalid.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when property names are null or whitespace.</exception>
        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(RouteIdPropertyName))
            {
                throw new ArgumentException("RouteId property name cannot be null or whitespace.", nameof(RouteIdPropertyName));
            }

            if (string.IsNullOrWhiteSpace(ClusterIdPropertyName))
            {
                throw new ArgumentException("ClusterId property name cannot be null or whitespace.", nameof(ClusterIdPropertyName));
            }

            if (string.IsNullOrWhiteSpace(DestinationIdPropertyName))
            {
                throw new ArgumentException("DestinationId property name cannot be null or whitespace.", nameof(DestinationIdPropertyName));
            }
        }
    }
}
