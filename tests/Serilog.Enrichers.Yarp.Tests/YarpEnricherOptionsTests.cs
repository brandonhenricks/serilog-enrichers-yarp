using FluentAssertions;
using System;

namespace Serilog.Enrichers.Yarp.Tests
{
    public class YarpEnricherOptionsTests
    {
        [Fact]
        public void DefaultOptions_ShouldHaveCorrectDefaults()
        {
            // Arrange & Act
            var options = new YarpEnricherOptions();

            // Assert
            options.IncludeRouteId.Should().BeTrue();
            options.IncludeClusterId.Should().BeTrue();
            options.IncludeDestinationId.Should().BeTrue();
            options.IncludeCorrelationContext.Should().BeTrue();
            options.RouteIdPropertyName.Should().Be("YarpRouteId");
            options.ClusterIdPropertyName.Should().Be("YarpClusterId");
            options.DestinationIdPropertyName.Should().Be("YarpDestinationId");
        }

        [Fact]
        public void Validate_WithValidOptions_ShouldNotThrow()
        {
            // Arrange
            var options = new YarpEnricherOptions();

            // Act
            var act = () => options.Validate();

            // Assert
            act.Should().NotThrow();
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void Validate_WithInvalidRouteIdPropertyName_ShouldThrow(string? propertyName)
        {
            // Arrange
            var options = new YarpEnricherOptions
            {
                RouteIdPropertyName = propertyName!
            };

            // Act
            var act = () => options.Validate();

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("*RouteId property name cannot be null or whitespace*");
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void Validate_WithInvalidClusterIdPropertyName_ShouldThrow(string? propertyName)
        {
            // Arrange
            var options = new YarpEnricherOptions
            {
                ClusterIdPropertyName = propertyName!
            };

            // Act
            var act = () => options.Validate();

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("*ClusterId property name cannot be null or whitespace*");
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void Validate_WithInvalidDestinationIdPropertyName_ShouldThrow(string? propertyName)
        {
            // Arrange
            var options = new YarpEnricherOptions
            {
                DestinationIdPropertyName = propertyName!
            };

            // Act
            var act = () => options.Validate();

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("*DestinationId property name cannot be null or whitespace*");
        }

        [Fact]
        public void Options_ShouldAllowCustomization()
        {
            // Arrange
            var options = new YarpEnricherOptions
            {
                IncludeRouteId = false,
                IncludeClusterId = false,
                IncludeDestinationId = false,
                IncludeCorrelationContext = false,
                RouteIdPropertyName = "CustomRoute",
                ClusterIdPropertyName = "CustomCluster",
                DestinationIdPropertyName = "CustomDestination"
            };

            // Act & Assert
            options.IncludeRouteId.Should().BeFalse();
            options.IncludeClusterId.Should().BeFalse();
            options.IncludeDestinationId.Should().BeFalse();
            options.IncludeCorrelationContext.Should().BeFalse();
            options.RouteIdPropertyName.Should().Be("CustomRoute");
            options.ClusterIdPropertyName.Should().Be("CustomCluster");
            options.DestinationIdPropertyName.Should().Be("CustomDestination");
        }
    }
}
