using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using Serilog.Events;
using Serilog.Sinks.InMemory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Forwarder;
using Yarp.ReverseProxy.Model;

namespace Serilog.Enrichers.Yarp.Tests
{
    /// <summary>
    /// Tests to verify that exceptions occurring during YARP proxy operations
    /// are enriched with YARP metadata (RouteId, ClusterId, DestinationId).
    /// </summary>
    public class YarpLogEnricherExceptionTests : IDisposable
    {
        public YarpLogEnricherExceptionTests()
        {
            // Clear the singleton InMemorySink before each test to ensure test isolation
            InMemorySink.Instance.Dispose();
        }

        public void Dispose()
        {
            // Clean up resources after test if needed
        }

        [Fact]
        public void Enrich_LogEventWithHttpRequestException_ShouldIncludeYarpMetadata()
        {
            // Arrange
            var mockAccessor = new Mock<IHttpContextAccessor>();
            var httpContext = new DefaultHttpContext();
            httpContext.TraceIdentifier = "trace-exception-001";

            // Create YARP feature
            var reverseProxyFeature = CreateMockReverseProxyFeature(
                routeId: "exception-route",
                clusterId: "exception-cluster",
                destinationId: "exception-destination"
            );

            httpContext.Features.Set(reverseProxyFeature);
            mockAccessor.Setup(x => x.HttpContext).Returns(httpContext);

            var enricher = new YarpLogEnricher(mockAccessor.Object);

            var logger = new LoggerConfiguration()
                .Enrich.With(enricher)
                .WriteTo.InMemory()
                .CreateLogger();

            // Act - Log an exception
            var exception = new HttpRequestException("Connection timeout to backend server");
            logger.Error(exception, "Failed to proxy request to backend");

            // Assert
            var logEvents = InMemorySink.Instance.LogEvents.ToList();
            logEvents.Should().HaveCount(1);
            var logEvent = logEvents[0];

            // Verify exception is logged
            logEvent.Exception.Should().NotBeNull();
            logEvent.Exception.Should().BeOfType<HttpRequestException>();
            logEvent.Exception.Message.Should().Be("Connection timeout to backend server");

            // Verify YARP metadata is enriched
            logEvent.Properties.Should().ContainKey("YarpRouteId");
            logEvent.Properties["YarpRouteId"].ToString().Should().Be("\"exception-route\"");

            logEvent.Properties.Should().ContainKey("YarpClusterId");
            logEvent.Properties["YarpClusterId"].ToString().Should().Be("\"exception-cluster\"");

            logEvent.Properties.Should().ContainKey("YarpDestinationId");
            logEvent.Properties["YarpDestinationId"].ToString().Should().Be("\"exception-destination\"");

            logEvent.Properties.Should().ContainKey("TraceIdentifier");
            logEvent.Properties["TraceIdentifier"].ToString().Should().Be("\"trace-exception-001\"");
        }

        [Fact]
        public void Enrich_LogEventWithTaskCanceledException_ShouldIncludeYarpMetadata()
        {
            // Arrange
            var mockAccessor = new Mock<IHttpContextAccessor>();
            var httpContext = new DefaultHttpContext();

            var reverseProxyFeature = CreateMockReverseProxyFeature(
                routeId: "timeout-route",
                clusterId: "timeout-cluster",
                destinationId: "timeout-destination"
            );

            httpContext.Features.Set(reverseProxyFeature);
            mockAccessor.Setup(x => x.HttpContext).Returns(httpContext);

            var enricher = new YarpLogEnricher(mockAccessor.Object);

            var logger = new LoggerConfiguration()
                .Enrich.With(enricher)
                .WriteTo.InMemory()
                .CreateLogger();

            // Act - Log a TaskCanceledException
            var exception = new TaskCanceledException("Request was canceled due to timeout");
            logger.Warning(exception, "Proxy request was canceled");

            // Assert
            var logEvents = InMemorySink.Instance.LogEvents.ToList();
            logEvents.Should().HaveCount(1);
            var logEvent = logEvents[0];

            logEvent.Exception.Should().NotBeNull();
            logEvent.Exception.Should().BeOfType<TaskCanceledException>();

            logEvent.Properties.Should().ContainKey("YarpRouteId");
            logEvent.Properties["YarpRouteId"].ToString().Should().Be("\"timeout-route\"");

            logEvent.Properties.Should().ContainKey("YarpClusterId");
            logEvent.Properties["YarpClusterId"].ToString().Should().Be("\"timeout-cluster\"");

            logEvent.Properties.Should().ContainKey("YarpDestinationId");
            logEvent.Properties["YarpDestinationId"].ToString().Should().Be("\"timeout-destination\"");
        }

        [Fact]
        public void Enrich_LogEventWithTimeoutException_ShouldIncludeYarpMetadata()
        {
            // Arrange
            var mockAccessor = new Mock<IHttpContextAccessor>();
            var httpContext = new DefaultHttpContext();

            var reverseProxyFeature = CreateMockReverseProxyFeature(
                routeId: "slow-route",
                clusterId: "slow-cluster",
                destinationId: "slow-destination"
            );

            httpContext.Features.Set(reverseProxyFeature);
            mockAccessor.Setup(x => x.HttpContext).Returns(httpContext);

            var enricher = new YarpLogEnricher(mockAccessor.Object);

            var logger = new LoggerConfiguration()
                .Enrich.With(enricher)
                .WriteTo.InMemory()
                .CreateLogger();

            // Act - Log a TimeoutException
            var exception = new TimeoutException("Operation timed out after 30 seconds");
            logger.Error(exception, "Backend service did not respond in time");

            // Assert
            var logEvents = InMemorySink.Instance.LogEvents.ToList();
            logEvents.Should().HaveCount(1);
            var logEvent = logEvents[0];

            logEvent.Exception.Should().NotBeNull();
            logEvent.Exception.Should().BeOfType<TimeoutException>();

            logEvent.Properties.Should().ContainKey("YarpRouteId");
            logEvent.Properties["YarpRouteId"].ToString().Should().Be("\"slow-route\"");

            logEvent.Properties.Should().ContainKey("YarpClusterId");
            logEvent.Properties["YarpClusterId"].ToString().Should().Be("\"slow-cluster\"");

            logEvent.Properties.Should().ContainKey("YarpDestinationId");
            logEvent.Properties["YarpDestinationId"].ToString().Should().Be("\"slow-destination\"");
        }

        [Fact]
        public void Enrich_LogEventWithInvalidOperationException_ShouldIncludeYarpMetadata()
        {
            // Arrange
            var mockAccessor = new Mock<IHttpContextAccessor>();
            var httpContext = new DefaultHttpContext();

            var reverseProxyFeature = CreateMockReverseProxyFeature(
                routeId: "invalid-route",
                clusterId: "invalid-cluster",
                destinationId: "invalid-destination"
            );

            httpContext.Features.Set(reverseProxyFeature);
            mockAccessor.Setup(x => x.HttpContext).Returns(httpContext);

            var enricher = new YarpLogEnricher(mockAccessor.Object);

            var logger = new LoggerConfiguration()
                .Enrich.With(enricher)
                .WriteTo.InMemory()
                .CreateLogger();

            // Act - Log an InvalidOperationException
            var exception = new InvalidOperationException("No healthy destinations available");
            logger.Error(exception, "Failed to select destination for proxy");

            // Assert
            var logEvents = InMemorySink.Instance.LogEvents.ToList();
            logEvents.Should().HaveCount(1);
            var logEvent = logEvents[0];

            logEvent.Exception.Should().NotBeNull();
            logEvent.Exception.Should().BeOfType<InvalidOperationException>();

            logEvent.Properties.Should().ContainKey("YarpRouteId");
            logEvent.Properties["YarpRouteId"].ToString().Should().Be("\"invalid-route\"");

            logEvent.Properties.Should().ContainKey("YarpClusterId");
            logEvent.Properties["YarpClusterId"].ToString().Should().Be("\"invalid-cluster\"");

            logEvent.Properties.Should().ContainKey("YarpDestinationId");
            logEvent.Properties["YarpDestinationId"].ToString().Should().Be("\"invalid-destination\"");
        }

        [Fact]
        public void Enrich_LogEventWithAggregateException_ShouldIncludeYarpMetadata()
        {
            // Arrange
            var mockAccessor = new Mock<IHttpContextAccessor>();
            var httpContext = new DefaultHttpContext();

            var reverseProxyFeature = CreateMockReverseProxyFeature(
                routeId: "aggregate-route",
                clusterId: "aggregate-cluster",
                destinationId: "aggregate-destination"
            );

            httpContext.Features.Set(reverseProxyFeature);
            mockAccessor.Setup(x => x.HttpContext).Returns(httpContext);

            var enricher = new YarpLogEnricher(mockAccessor.Object);

            var logger = new LoggerConfiguration()
                .Enrich.With(enricher)
                .WriteTo.InMemory()
                .CreateLogger();

            // Act - Log an AggregateException
            var innerExceptions = new[]
            {
                new HttpRequestException("Failed to connect to server 1"),
                new HttpRequestException("Failed to connect to server 2")
            };
            var exception = new AggregateException("Multiple proxy failures", innerExceptions);
            logger.Error(exception, "All proxy attempts failed");

            // Assert
            var logEvents = InMemorySink.Instance.LogEvents.ToList();
            logEvents.Should().HaveCount(1);
            var logEvent = logEvents[0];

            logEvent.Exception.Should().NotBeNull();
            logEvent.Exception.Should().BeOfType<AggregateException>();

            logEvent.Properties.Should().ContainKey("YarpRouteId");
            logEvent.Properties["YarpRouteId"].ToString().Should().Be("\"aggregate-route\"");

            logEvent.Properties.Should().ContainKey("YarpClusterId");
            logEvent.Properties["YarpClusterId"].ToString().Should().Be("\"aggregate-cluster\"");

            logEvent.Properties.Should().ContainKey("YarpDestinationId");
            logEvent.Properties["YarpDestinationId"].ToString().Should().Be("\"aggregate-destination\"");
        }

        [Fact]
        public void Enrich_LogEventWithOperationCanceledException_ShouldIncludeYarpMetadata()
        {
            // Arrange
            var mockAccessor = new Mock<IHttpContextAccessor>();
            var httpContext = new DefaultHttpContext();

            var reverseProxyFeature = CreateMockReverseProxyFeature(
                routeId: "cancelled-route",
                clusterId: "cancelled-cluster",
                destinationId: "cancelled-destination"
            );

            httpContext.Features.Set(reverseProxyFeature);
            mockAccessor.Setup(x => x.HttpContext).Returns(httpContext);

            var enricher = new YarpLogEnricher(mockAccessor.Object);

            var logger = new LoggerConfiguration()
                .Enrich.With(enricher)
                .WriteTo.InMemory()
                .CreateLogger();

            // Act - Log an OperationCanceledException
            var exception = new OperationCanceledException("Proxy operation was canceled");
            logger.Warning(exception, "Request handling was canceled");

            // Assert
            var logEvents = InMemorySink.Instance.LogEvents.ToList();
            logEvents.Should().HaveCount(1);
            var logEvent = logEvents[0];

            logEvent.Exception.Should().NotBeNull();
            logEvent.Exception.Should().BeOfType<OperationCanceledException>();

            logEvent.Properties.Should().ContainKey("YarpRouteId");
            logEvent.Properties["YarpRouteId"].ToString().Should().Be("\"cancelled-route\"");

            logEvent.Properties.Should().ContainKey("YarpClusterId");
            logEvent.Properties["YarpClusterId"].ToString().Should().Be("\"cancelled-cluster\"");

            logEvent.Properties.Should().ContainKey("YarpDestinationId");
            logEvent.Properties["YarpDestinationId"].ToString().Should().Be("\"cancelled-destination\"");
        }

        [Fact]
        public void Enrich_ExceptionWithoutHttpContext_ShouldNotAddYarpProperties()
        {
            // Arrange - Simulate scenario where HttpContext is not available
            var mockAccessor = new Mock<IHttpContextAccessor>();
            mockAccessor.Setup(x => x.HttpContext).Returns((HttpContext?)null);

            var enricher = new YarpLogEnricher(mockAccessor.Object);

            var logger = new LoggerConfiguration()
                .Enrich.With(enricher)
                .WriteTo.InMemory()
                .CreateLogger();

            // Act - Log an exception without HttpContext
            var exception = new HttpRequestException("No HttpContext available");
            logger.Error(exception, "Exception occurred outside HTTP request context");

            // Assert
            var logEvents = InMemorySink.Instance.LogEvents.ToList();
            logEvents.Should().HaveCount(1);
            var logEvent = logEvents[0];

            logEvent.Exception.Should().NotBeNull();

            // YARP properties should NOT be present
            logEvent.Properties.Should().NotContainKey("YarpRouteId");
            logEvent.Properties.Should().NotContainKey("YarpClusterId");
            logEvent.Properties.Should().NotContainKey("YarpDestinationId");
        }

        [Fact]
        public void Enrich_ExceptionWithoutYarpFeature_ShouldNotAddYarpProperties()
        {
            // Arrange - HttpContext exists but no YARP feature
            var mockAccessor = new Mock<IHttpContextAccessor>();
            var httpContext = new DefaultHttpContext();
            httpContext.TraceIdentifier = "trace-no-yarp";

            mockAccessor.Setup(x => x.HttpContext).Returns(httpContext);

            var enricher = new YarpLogEnricher(mockAccessor.Object);

            var logger = new LoggerConfiguration()
                .Enrich.With(enricher)
                .WriteTo.InMemory()
                .CreateLogger();

            // Act - Log an exception without YARP feature
            var exception = new InvalidOperationException("Not a YARP request");
            logger.Error(exception, "Exception in non-YARP request");

            // Assert
            var logEvents = InMemorySink.Instance.LogEvents.ToList();
            logEvents.Should().HaveCount(1);
            var logEvent = logEvents[0];

            logEvent.Exception.Should().NotBeNull();

            // YARP properties should NOT be present (no YARP feature)
            logEvent.Properties.Should().NotContainKey("YarpRouteId");
            logEvent.Properties.Should().NotContainKey("YarpClusterId");
            logEvent.Properties.Should().NotContainKey("YarpDestinationId");

            // But TraceIdentifier should still be present
            logEvent.Properties.Should().ContainKey("TraceIdentifier");
        }

        [Fact]
        public void Enrich_ExceptionWithPartialYarpContext_ShouldAddOnlyAvailableProperties()
        {
            // Arrange - YARP feature with only partial metadata
            var mockAccessor = new Mock<IHttpContextAccessor>();
            var httpContext = new DefaultHttpContext();

            var reverseProxyFeature = CreateMockReverseProxyFeature(
                routeId: "partial-route",
                clusterId: null, // No cluster info
                destinationId: null  // No destination info
            );

            httpContext.Features.Set(reverseProxyFeature);
            mockAccessor.Setup(x => x.HttpContext).Returns(httpContext);

            var enricher = new YarpLogEnricher(mockAccessor.Object);

            var logger = new LoggerConfiguration()
                .Enrich.With(enricher)
                .WriteTo.InMemory()
                .CreateLogger();

            // Act - Log an exception with partial YARP context
            var exception = new HttpRequestException("Partial context exception");
            logger.Error(exception, "Exception with partial YARP metadata");

            // Assert
            var logEvents = InMemorySink.Instance.LogEvents.ToList();
            logEvents.Should().HaveCount(1);
            var logEvent = logEvents[0];

            logEvent.Exception.Should().NotBeNull();

            // Only RouteId should be present
            logEvent.Properties.Should().ContainKey("YarpRouteId");
            logEvent.Properties["YarpRouteId"].ToString().Should().Be("\"partial-route\"");

            // Others should not be present
            logEvent.Properties.Should().NotContainKey("YarpClusterId");
            logEvent.Properties.Should().NotContainKey("YarpDestinationId");
        }

        [Fact]
        public void Enrich_MultipleExceptionsInSameContext_ShouldAllBeEnrichedWithSameYarpMetadata()
        {
            // Arrange
            var mockAccessor = new Mock<IHttpContextAccessor>();
            var httpContext = new DefaultHttpContext();

            var reverseProxyFeature = CreateMockReverseProxyFeature(
                routeId: "multi-exception-route",
                clusterId: "multi-exception-cluster",
                destinationId: "multi-exception-destination"
            );

            httpContext.Features.Set(reverseProxyFeature);
            mockAccessor.Setup(x => x.HttpContext).Returns(httpContext);

            var enricher = new YarpLogEnricher(mockAccessor.Object);

            var logger = new LoggerConfiguration()
                .Enrich.With(enricher)
                .WriteTo.InMemory()
                .CreateLogger();

            // Act - Log multiple different exceptions in the same context
            logger.Error(new HttpRequestException("First error"), "First proxy failure");
            logger.Warning(new TaskCanceledException("Second error"), "Second proxy issue");
            logger.Error(new TimeoutException("Third error"), "Third proxy timeout");

            // Assert
            var logEvents = InMemorySink.Instance.LogEvents.ToList();
            logEvents.Should().HaveCount(3);

            // All three log events should have the same YARP metadata
            foreach (var logEvent in logEvents)
            {
                logEvent.Exception.Should().NotBeNull();

                logEvent.Properties.Should().ContainKey("YarpRouteId");
                logEvent.Properties["YarpRouteId"].ToString().Should().Be("\"multi-exception-route\"");

                logEvent.Properties.Should().ContainKey("YarpClusterId");
                logEvent.Properties["YarpClusterId"].ToString().Should().Be("\"multi-exception-cluster\"");

                logEvent.Properties.Should().ContainKey("YarpDestinationId");
                logEvent.Properties["YarpDestinationId"].ToString().Should().Be("\"multi-exception-destination\"");
            }
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
