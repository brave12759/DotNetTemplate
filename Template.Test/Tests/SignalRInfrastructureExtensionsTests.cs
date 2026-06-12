using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Template.Common.BackgroundQueue;
using Template.WebApi.Extensions;
using Template.WebApi.SignalR;

namespace Template.Test.Tests;

[TestClass]
public class SignalRInfrastructureExtensionsTests
{
    [TestMethod]
    public void AddSignalRInfrastructure_Should_RegisterQueuedSignalRMessageHandler()
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddSignalRInfrastructure();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var handlers = scope.ServiceProvider.GetServices<IBackgroundJobHandler>().ToList();

        Assert.IsTrue(handlers.Any(h => h.GetType() == typeof(QueuedSignalRMessageHandler)));
    }

    [TestMethod]
    public void MapSignalRInfrastructure_Should_MapNotificationHubEndpoint()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddSignalR();

        var app = builder.Build();
        app.MapSignalRInfrastructure();

        var endpointRouteBuilder = (IEndpointRouteBuilder)app;

        var routeEndpoints = endpointRouteBuilder.DataSources
            .SelectMany(ds => ds.Endpoints)
            .OfType<RouteEndpoint>()
            .ToList();

        Assert.IsTrue(routeEndpoints.Any(e => string.Equals(e.RoutePattern.RawText, "/hubs/notifications", StringComparison.OrdinalIgnoreCase)));
    }
}
