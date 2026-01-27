using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Serilog.Enrichers.Yarp.Tests
{
    public class YarpEnricherServiceCollectionExtensionsTests
    {
        [Fact]
        public void AddYarpLogEnricher_WithNullServiceCollection_ShouldThrow()
        {
            // Arrange, Act & Assert
            var act = () => ((IServiceCollection)null!).AddYarpLogEnricher();
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("services");
        }

        [Fact]
        public void AddYarpLogEnricher_ShouldRegisterHttpContextAccessor()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddYarpLogEnricher();

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var httpContextAccessor = serviceProvider.GetService<IHttpContextAccessor>();
            httpContextAccessor.Should().NotBeNull();
        }

        [Fact]
        public void AddYarpLogEnricher_WithExistingHttpContextAccessor_ShouldNotOverride()
        {
            // Arrange
            var services = new ServiceCollection();
            var customAccessor = new HttpContextAccessor();
            services.AddSingleton<IHttpContextAccessor>(customAccessor);

            // Act
            services.AddYarpLogEnricher();

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var httpContextAccessor = serviceProvider.GetService<IHttpContextAccessor>();
            httpContextAccessor.Should().BeSameAs(customAccessor);
        }

        [Fact]
        public void AddYarpLogEnricher_WithConfigureAction_ShouldRegisterOptions()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddYarpLogEnricher(opts =>
            {
                opts.IncludeRouteId = false;
                opts.RouteIdPropertyName = "CustomRoute";
            });

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var options = serviceProvider.GetService<YarpEnricherOptions>();
            options.Should().NotBeNull();
            options!.IncludeRouteId.Should().BeFalse();
            options.RouteIdPropertyName.Should().Be("CustomRoute");
        }

        [Fact]
        public void AddYarpLogEnricher_WithNullConfigureAction_ShouldThrow()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act & Assert
            var act = () => services.AddYarpLogEnricher((Action<YarpEnricherOptions>)null!);
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("configureOptions");
        }

        [Fact]
        public void AddYarpLogEnricher_WithInvalidOptions_ShouldThrow()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act & Assert
            var act = () => services.AddYarpLogEnricher(opts =>
            {
                opts.RouteIdPropertyName = ""; // Invalid
            });
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void AddYarpLogEnricher_ShouldReturnServiceCollection()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            var result = services.AddYarpLogEnricher();

            // Assert
            result.Should().BeSameAs(services);
        }

        [Fact]
        public void AddYarpLogEnricher_WithConfigureAction_ShouldReturnServiceCollection()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            var result = services.AddYarpLogEnricher(opts => opts.IncludeRouteId = true);

            // Assert
            result.Should().BeSameAs(services);
        }
    }
}
