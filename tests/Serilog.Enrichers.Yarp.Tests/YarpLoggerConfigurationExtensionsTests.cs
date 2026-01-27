using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using Serilog.Configuration;
using System;

namespace Serilog.Enrichers.Yarp.Tests
{
    public class YarpLoggerConfigurationExtensionsTests
    {
        [Fact]
        public void WithYarpContext_WithNullEnrichmentConfiguration_ShouldThrow()
        {
            // Arrange
            var mockAccessor = new Mock<IHttpContextAccessor>();

            // Act & Assert
            var act = () => ((LoggerEnrichmentConfiguration)null!).WithYarpContext(mockAccessor.Object);
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("enrichmentConfiguration");
        }

        [Fact]
        public void WithYarpContext_WithNullHttpContextAccessor_ShouldThrow()
        {
            // Arrange
            var loggerConfig = new LoggerConfiguration();

            // Act & Assert
            var act = () => loggerConfig.Enrich.WithYarpContext(null!);
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("httpContextAccessor");
        }

        [Fact]
        public void WithYarpContext_WithValidParameters_ShouldSucceed()
        {
            // Arrange
            var mockAccessor = new Mock<IHttpContextAccessor>();
            var loggerConfig = new LoggerConfiguration();

            // Act
            var result = loggerConfig.Enrich.WithYarpContext(mockAccessor.Object);

            // Assert
            result.Should().NotBeNull();
        }

        [Fact]
        public void WithYarpContext_WithOptions_ShouldSucceed()
        {
            // Arrange
            var mockAccessor = new Mock<IHttpContextAccessor>();
            var options = new YarpEnricherOptions();
            var loggerConfig = new LoggerConfiguration();

            // Act
            var result = loggerConfig.Enrich.WithYarpContext(mockAccessor.Object, options);

            // Assert
            result.Should().NotBeNull();
        }

        [Fact]
        public void WithYarpContext_WithConfigureAction_ShouldSucceed()
        {
            // Arrange
            var mockAccessor = new Mock<IHttpContextAccessor>();
            var loggerConfig = new LoggerConfiguration();

            // Act
            var result = loggerConfig.Enrich.WithYarpContext(mockAccessor.Object, opts =>
            {
                opts.IncludeRouteId = false;
                opts.RouteIdPropertyName = "CustomRoute";
            });

            // Assert
            result.Should().NotBeNull();
        }

        [Fact]
        public void WithYarpContext_WithNullConfigureAction_ShouldThrow()
        {
            // Arrange
            var mockAccessor = new Mock<IHttpContextAccessor>();
            var loggerConfig = new LoggerConfiguration();

            // Act & Assert
            var act = () => loggerConfig.Enrich.WithYarpContext(mockAccessor.Object, (Action<YarpEnricherOptions>)null!);
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("configureOptions");
        }

        [Fact]
        public void WithYarpContext_WithInvalidOptions_ShouldThrow()
        {
            // Arrange
            var mockAccessor = new Mock<IHttpContextAccessor>();
            var options = new YarpEnricherOptions
            {
                RouteIdPropertyName = "" // Invalid
            };
            var loggerConfig = new LoggerConfiguration();

            // Act & Assert
            var act = () => loggerConfig.Enrich.WithYarpContext(mockAccessor.Object, options);
            act.Should().Throw<ArgumentException>();
        }
    }
}
