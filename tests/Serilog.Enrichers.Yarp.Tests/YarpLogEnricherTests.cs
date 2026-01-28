using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.InMemory;
using System;
using System.Collections.Generic;
using System.Linq;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Forwarder;
using Yarp.ReverseProxy.Model;

namespace Serilog.Enrichers.Yarp.Tests
{
    public class YarpLogEnricherTests : IDisposable
    {
        public YarpLogEnricherTests()
        {
            // Clear the singleton InMemorySink before each test to ensure test isolation
            InMemorySink.Instance.Dispose();
        }

        public void Dispose()
        {
            // Clean up resources after test if needed
        }

        [Fact]
        public void Constructor_WithNullHttpContextAccessor_ShouldThrow()
        {
            // Arrange, Act & Assert
            var act = () => new YarpLogEnricher(null!);

            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("httpContextAccessor");
        }

        [Fact]
        public void Constructor_WithValidParameters_ShouldSucceed()
        {
            // Arrange
            var mockAccessor = new Mock<IHttpContextAccessor>();
            var options = new YarpEnricherOptions();

            // Act
            var enricher = new YarpLogEnricher(mockAccessor.Object, options);

            // Assert
            enricher.Should().NotBeNull();
        }

        [Fact]
        public void Constructor_WithInvalidOptions_ShouldThrow()
        {
            // Arrange
            var mockAccessor = new Mock<IHttpContextAccessor>();
            var options = new YarpEnricherOptions
            {
                RouteIdPropertyName = ""
            };

            // Act & Assert
            var act = () => new YarpLogEnricher(mockAccessor.Object, options);

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Enrich_WithNullHttpContext_ShouldNotAddProperties()
        {
            // Arrange
            var mockAccessor = new Mock<IHttpContextAccessor>();
            mockAccessor.Setup(x => x.HttpContext).Returns((HttpContext?)null!);

            var enricher = new YarpLogEnricher(mockAccessor.Object);

            var logger = new LoggerConfiguration()
                .Enrich.With(enricher)
                .WriteTo.InMemory()
                .CreateLogger();

            // Act
            logger.Information("Test message");

            // Assert
            var logEvents = InMemorySink.Instance.LogEvents.ToList();
            logEvents.Should().HaveCount(1);
            var logEvent = logEvents[0];
            logEvent.Properties.Should().NotContainKey("YarpRouteId");
            logEvent.Properties.Should().NotContainKey("YarpClusterId");
            logEvent.Properties.Should().NotContainKey("YarpDestinationId");
        }

        [Fact]
        public void Enrich_WithNullLogEvent_ShouldThrow()
        {
            // Arrange
            var mockAccessor = new Mock<IHttpContextAccessor>();
            var enricher = new YarpLogEnricher(mockAccessor.Object);
            var mockFactory = new Mock<ILogEventPropertyFactory>();

            // Act & Assert
            var act = () => enricher.Enrich(null!, mockFactory.Object);
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("logEvent");
        }

        [Fact]
        public void Enrich_WithNullPropertyFactory_ShouldThrow()
        {
            // Arrange
            var mockAccessor = new Mock<IHttpContextAccessor>();
            var enricher = new YarpLogEnricher(mockAccessor.Object);
            var logEvent = new LogEvent(
                DateTimeOffset.Now,
                LogEventLevel.Information,
                null,
                MessageTemplate.Empty,
                Enumerable.Empty<LogEventProperty>());

            // Act & Assert
            var act = () => enricher.Enrich(logEvent, null!);
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("propertyFactory");
        }

        [Fact]
        public void Enrich_WithYarpContextInFeatures_ShouldAddProperties()
        {
            // Arrange
            var mockAccessor = new Mock<IHttpContextAccessor>();
            var httpContext = new DefaultHttpContext();
            httpContext.TraceIdentifier = "trace-123";

            // Create YARP feature
            var reverseProxyFeature = CreateMockReverseProxyFeature(
                routeId: "test-route",
                clusterId: "test-cluster",
                destinationId: "test-destination"
            );

            httpContext.Features.Set(reverseProxyFeature);
            mockAccessor.Setup(x => x.HttpContext).Returns(httpContext);

            var enricher = new YarpLogEnricher(mockAccessor.Object);

            var logger = new LoggerConfiguration()
                .Enrich.With(enricher)
                .WriteTo.InMemory()
                .CreateLogger();

            // Act
            logger.Information("Test message");

            // Assert
            var logEvents = InMemorySink.Instance.LogEvents.ToList();
            logEvents.Should().HaveCount(1);
            var logEvent = logEvents[0];
            
            logEvent.Properties.Should().ContainKey("YarpRouteId");
            logEvent.Properties["YarpRouteId"].ToString().Should().Be("\"test-route\"");
            
            logEvent.Properties.Should().ContainKey("YarpClusterId");
            logEvent.Properties["YarpClusterId"].ToString().Should().Be("\"test-cluster\"");
            
            logEvent.Properties.Should().ContainKey("YarpDestinationId");
            logEvent.Properties["YarpDestinationId"].ToString().Should().Be("\"test-destination\"");
            
            logEvent.Properties.Should().ContainKey("TraceIdentifier");
            logEvent.Properties["TraceIdentifier"].ToString().Should().Be("\"trace-123\"");
        }

        [Fact]
        public void Enrich_WithYarpContextInFeatures_ShouldAddPropertiesWithAlternativeValues()
        {
            // Arrange - This test verifies the enricher works with different values
            var mockAccessor = new Mock<IHttpContextAccessor>();
            var httpContext = new DefaultHttpContext();

            // Create YARP feature
            var reverseProxyFeature = CreateMockReverseProxyFeature(
                routeId: "alt-route",
                clusterId: "alt-cluster",
                destinationId: "alt-destination"
            );

            httpContext.Features.Set(reverseProxyFeature);
            mockAccessor.Setup(x => x.HttpContext).Returns(httpContext);

            var enricher = new YarpLogEnricher(mockAccessor.Object);

            var logger = new LoggerConfiguration()
                .Enrich.With(enricher)
                .WriteTo.InMemory()
                .CreateLogger();

            // Act
            logger.Information("Test message");

            // Assert
            var logEvents = InMemorySink.Instance.LogEvents.ToList();
            logEvents.Should().HaveCount(1);
            var logEvent = logEvents[0];
            
            logEvent.Properties.Should().ContainKey("YarpRouteId");
            logEvent.Properties["YarpRouteId"].ToString().Should().Be("\"alt-route\"");
            
            logEvent.Properties.Should().ContainKey("YarpClusterId");
            logEvent.Properties["YarpClusterId"].ToString().Should().Be("\"alt-cluster\"");
            
            logEvent.Properties.Should().ContainKey("YarpDestinationId");
            logEvent.Properties["YarpDestinationId"].ToString().Should().Be("\"alt-destination\"");
        }

        [Fact]
        public void Enrich_WithCustomOptions_ShouldUseCustomPropertyNames()
        {
            // Arrange
            var mockAccessor = new Mock<IHttpContextAccessor>();
            var httpContext = new DefaultHttpContext();

            // Create YARP feature
            var reverseProxyFeature = CreateMockReverseProxyFeature(
                routeId: "test-route",
                clusterId: "test-cluster",
                destinationId: "test-destination"
            );

            httpContext.Features.Set(reverseProxyFeature);
            mockAccessor.Setup(x => x.HttpContext).Returns(httpContext);

            var options = new YarpEnricherOptions
            {
                RouteIdPropertyName = "CustomRoute",
                ClusterIdPropertyName = "CustomCluster",
                DestinationIdPropertyName = "CustomDestination"
            };

            var enricher = new YarpLogEnricher(mockAccessor.Object, options);

            var logger = new LoggerConfiguration()
                .Enrich.With(enricher)
                .WriteTo.InMemory()
                .CreateLogger();

            // Act
            logger.Information("Test message");

            // Assert
            var logEvents = InMemorySink.Instance.LogEvents.ToList();
            logEvents.Should().HaveCount(1);
            var logEvent = logEvents[0];
            
            logEvent.Properties.Should().ContainKey("CustomRoute");
            logEvent.Properties["CustomRoute"].ToString().Should().Be("\"test-route\"");
            
            logEvent.Properties.Should().ContainKey("CustomCluster");
            logEvent.Properties["CustomCluster"].ToString().Should().Be("\"test-cluster\"");
            
            logEvent.Properties.Should().ContainKey("CustomDestination");
            logEvent.Properties["CustomDestination"].ToString().Should().Be("\"test-destination\"");
        }

        [Fact]
        public void Enrich_WithOptionsDisablingProperties_ShouldNotAddDisabledProperties()
        {
            // Arrange
            var mockAccessor = new Mock<IHttpContextAccessor>();
            var httpContext = new DefaultHttpContext();
            httpContext.TraceIdentifier = "trace-123";

            // Create YARP feature
            var reverseProxyFeature = CreateMockReverseProxyFeature(
                routeId: "test-route",
                clusterId: "test-cluster",
                destinationId: "test-destination"
            );

            httpContext.Features.Set(reverseProxyFeature);
            mockAccessor.Setup(x => x.HttpContext).Returns(httpContext);

            var options = new YarpEnricherOptions
            {
                IncludeRouteId = false,
                IncludeClusterId = true,
                IncludeDestinationId = false,
                IncludeCorrelationContext = false
            };

            var enricher = new YarpLogEnricher(mockAccessor.Object, options);

            var logger = new LoggerConfiguration()
                .Enrich.With(enricher)
                .WriteTo.InMemory()
                .CreateLogger();

            // Act
            logger.Information("Test message");

            // Assert
            var logEvents = InMemorySink.Instance.LogEvents.ToList();
            logEvents.Should().HaveCount(1);
            var logEvent = logEvents[0];
            
            logEvent.Properties.Should().NotContainKey("YarpRouteId");
            logEvent.Properties.Should().ContainKey("YarpClusterId");
            logEvent.Properties.Should().NotContainKey("YarpDestinationId");
            logEvent.Properties.Should().NotContainKey("TraceIdentifier");
        }

        [Fact]
        public void Enrich_WithNoYarpContext_ShouldNotAddYarpProperties()
        {
            // Arrange
            var mockAccessor = new Mock<IHttpContextAccessor>();
            var httpContext = new DefaultHttpContext();
            httpContext.TraceIdentifier = "trace-123";

            mockAccessor.Setup(x => x.HttpContext).Returns(httpContext);

            var enricher = new YarpLogEnricher(mockAccessor.Object);

            var logger = new LoggerConfiguration()
                .Enrich.With(enricher)
                .WriteTo.InMemory()
                .CreateLogger();

            // Act
            logger.Information("Test message");

            // Assert
            var logEvents = InMemorySink.Instance.LogEvents.ToList();
            logEvents.Should().HaveCount(1);
            var logEvent = logEvents[0];
            
            logEvent.Properties.Should().NotContainKey("YarpRouteId");
            logEvent.Properties.Should().NotContainKey("YarpClusterId");
            logEvent.Properties.Should().NotContainKey("YarpDestinationId");
            logEvent.Properties.Should().ContainKey("TraceIdentifier");
        }

        [Fact]
        public void Enrich_WithPartialYarpContext_ShouldAddOnlyAvailableProperties()
        {
            // Arrange
            var mockAccessor = new Mock<IHttpContextAccessor>();
            var httpContext = new DefaultHttpContext();

            // Create YARP feature with only RouteId
            var reverseProxyFeature = CreateMockReverseProxyFeature(
                routeId: "test-route",
                clusterId: null,
                destinationId: null
            );

            httpContext.Features.Set(reverseProxyFeature);
            mockAccessor.Setup(x => x.HttpContext).Returns(httpContext);

            var enricher = new YarpLogEnricher(mockAccessor.Object);

            var logger = new LoggerConfiguration()
                .Enrich.With(enricher)
                .WriteTo.InMemory()
                .CreateLogger();

            // Act
            logger.Information("Test message");

            // Assert
            var logEvents = InMemorySink.Instance.LogEvents.ToList();
            logEvents.Should().HaveCount(1);
            var logEvent = logEvents[0];
            
            logEvent.Properties.Should().ContainKey("YarpRouteId");
            logEvent.Properties.Should().NotContainKey("YarpClusterId");
            logEvent.Properties.Should().NotContainKey("YarpDestinationId");
        }

        /// <summary>
        /// Helper method to create a mock IReverseProxyFeature with specified values
        /// </summary>
        private IReverseProxyFeature CreateMockReverseProxyFeature(
            string? routeId = null, 
            string? clusterId = null, 
            string? destinationId = null)
        {
            return new TestReverseProxyFeature(routeId, clusterId, destinationId);
        }

        /// <summary>
        /// Test implementation of IReverseProxyFeature for testing
        /// </summary>
        private class TestReverseProxyFeature : IReverseProxyFeature
        {
            // Shared HttpMessageInvoker to avoid resource leaks in tests
            private static readonly HttpMessageInvoker SharedHttpClient = new HttpMessageInvoker(new SocketsHttpHandler());

            public TestReverseProxyFeature(string? routeId, string? clusterId, string? destinationId)
            {
                if (routeId != null)
                {
                    var routeConfig = new RouteConfig
                    {
                        RouteId = routeId,
                        Match = new RouteMatch(),
                        ClusterId = clusterId
                    };
                    Route = new RouteModel(routeConfig, null, HttpTransformer.Default);
                }

                if (clusterId != null)
                {
                    var clusterConfig = new ClusterConfig
                    {
                        ClusterId = clusterId
                    };
                    // Use shared HttpClient for testing to avoid resource leaks
                    Cluster = new ClusterModel(clusterConfig, SharedHttpClient);
                }

                if (destinationId != null)
                {
                    ProxiedDestination = new DestinationState(destinationId);
                }
            }

            public RouteModel? Route { get; }
            public ClusterModel? Cluster { get; }
            public IReadOnlyList<DestinationState>? AllDestinations => null;
            public IReadOnlyList<DestinationState>? AvailableDestinations { get; set; }
            public DestinationState? ProxiedDestination { get; set; }
        }
    }
}
