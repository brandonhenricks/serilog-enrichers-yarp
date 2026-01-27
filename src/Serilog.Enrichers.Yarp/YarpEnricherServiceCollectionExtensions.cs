using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;

namespace Serilog.Enrichers.Yarp
{
    /// <summary>
    /// Extension methods for registering YARP enricher services with dependency injection.
    /// </summary>
    public static class YarpEnricherServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the required services for YARP log enrichment to the service collection.
        /// This registers IHttpContextAccessor if not already registered.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when services is null.</exception>
        public static IServiceCollection AddYarpLogEnricher(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            // Register IHttpContextAccessor if not already registered
            services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            return services;
        }

        /// <summary>
        /// Adds the required services for YARP log enrichment to the service collection with custom options.
        /// This registers IHttpContextAccessor if not already registered and configures YarpEnricherOptions.
        /// Note: If this method is called multiple times, only the first call will register the options
        /// due to the use of TryAddSingleton. Subsequent calls will be silently ignored.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configureOptions">Action to configure the enricher options.</param>
        /// <returns>The service collection for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when services or configureOptions is null.</exception>
        public static IServiceCollection AddYarpLogEnricher(
            this IServiceCollection services,
            Action<YarpEnricherOptions> configureOptions)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (configureOptions == null)
            {
                throw new ArgumentNullException(nameof(configureOptions));
            }

            // Register IHttpContextAccessor if not already registered
            services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            // Configure options - only first call will be registered
            var options = new YarpEnricherOptions();
            configureOptions(options);
            options.Validate();

            services.TryAddSingleton(options);

            return services;
        }
    }
}
