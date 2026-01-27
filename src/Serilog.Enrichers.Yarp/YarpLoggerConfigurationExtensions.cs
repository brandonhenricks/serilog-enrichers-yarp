using Microsoft.AspNetCore.Http;
using Serilog.Configuration;
using System;

namespace Serilog.Enrichers.Yarp
{
    /// <summary>
    /// Extension methods for configuring the YARP enricher in Serilog.
    /// </summary>
    public static class YarpLoggerConfigurationExtensions
    {
        /// <summary>
        /// Enriches log events with YARP reverse proxy context (RouteId, ClusterId, DestinationId).
        /// </summary>
        /// <param name="enrichmentConfiguration">The logger enrichment configuration.</param>
        /// <param name="httpContextAccessor">The HTTP context accessor to retrieve the current request context.</param>
        /// <param name="options">Optional configuration options for the enricher.</param>
        /// <returns>Configuration object allowing method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when enrichmentConfiguration or httpContextAccessor is null.</exception>
        public static LoggerConfiguration WithYarpContext(
            this LoggerEnrichmentConfiguration enrichmentConfiguration,
            IHttpContextAccessor httpContextAccessor,
            YarpEnricherOptions? options = null)
        {
            if (enrichmentConfiguration == null)
            {
                throw new ArgumentNullException(nameof(enrichmentConfiguration));
            }

            if (httpContextAccessor == null)
            {
                throw new ArgumentNullException(nameof(httpContextAccessor));
            }

            return enrichmentConfiguration.With(new YarpLogEnricher(httpContextAccessor, options));
        }

        /// <summary>
        /// Enriches log events with YARP reverse proxy context (RouteId, ClusterId, DestinationId)
        /// using a custom options configuration action.
        /// </summary>
        /// <param name="enrichmentConfiguration">The logger enrichment configuration.</param>
        /// <param name="httpContextAccessor">The HTTP context accessor to retrieve the current request context.</param>
        /// <param name="configureOptions">Action to configure the enricher options.</param>
        /// <returns>Configuration object allowing method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when enrichmentConfiguration, httpContextAccessor, or configureOptions is null.</exception>
        public static LoggerConfiguration WithYarpContext(
            this LoggerEnrichmentConfiguration enrichmentConfiguration,
            IHttpContextAccessor httpContextAccessor,
            Action<YarpEnricherOptions> configureOptions)
        {
            if (enrichmentConfiguration == null)
            {
                throw new ArgumentNullException(nameof(enrichmentConfiguration));
            }

            if (httpContextAccessor == null)
            {
                throw new ArgumentNullException(nameof(httpContextAccessor));
            }

            if (configureOptions == null)
            {
                throw new ArgumentNullException(nameof(configureOptions));
            }

            var options = new YarpEnricherOptions();
            configureOptions(options);

            return enrichmentConfiguration.With(new YarpLogEnricher(httpContextAccessor, options));
        }
    }
}
