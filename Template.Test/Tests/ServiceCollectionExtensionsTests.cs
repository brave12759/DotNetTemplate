using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Template.BusinessRule.BackgroundQueue.Services;
using Template.BusinessRule.Extensions;
using Template.BusinessRule.TokenRevocationService.Services;
using Template.Common.BackgroundQueue;
using Template.Common.Settings;
using Template.DataAccess.Extensions;
using LogDb = Template.DataAccess.LogDbContext.LogDbContext;
using ProjectDb = Template.DataAccess.ProjectDbContext.ProjectDbContext;

namespace Template.Test.Tests;

[TestClass]
public class ServiceCollectionExtensionsTests
{
    [TestMethod]
    public void AddDataAccess_Should_RegisterProjectAndLogDbContext()
    {
        var services = new ServiceCollection();

        services.AddDataAccess(new DatabaseSettings
        {
            ProjectConnectionString = "Server=localhost;Database=Project;Trusted_Connection=True;TrustServerCertificate=True",
            LogConnectionString = "Server=localhost;Database=Log;Trusted_Connection=True;TrustServerCertificate=True"
        });

        using var provider = services.BuildServiceProvider();
        Assert.IsNotNull(provider.GetRequiredService<ProjectDb>());
        Assert.IsNotNull(provider.GetRequiredService<LogDb>());
    }

    [TestMethod]
    public void AddDataAccessHealthChecks_WithConnectionStrings_Should_RegisterBothChecks()
    {
        var services = new ServiceCollection();
        var builder = services.AddHealthChecks();

        builder.AddDataAccessHealthChecks(new DatabaseSettings
        {
            ProjectConnectionString = "project-conn",
            LogConnectionString = "log-conn"
        });

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value;

        Assert.IsTrue(options.Registrations.Any(r => r.Name == "project-db"));
        Assert.IsTrue(options.Registrations.Any(r => r.Name == "log-db"));
    }

    [TestMethod]
    public void AddDataAccessHealthChecks_EmptyConnectionStrings_Should_NotRegisterChecks()
    {
        var services = new ServiceCollection();
        var builder = services.AddHealthChecks();

        builder.AddDataAccessHealthChecks(new DatabaseSettings
        {
            ProjectConnectionString = "  ",
            LogConnectionString = string.Empty
        });

        using var provider = services.BuildServiceProvider();
        Assert.ThrowsException<InvalidOperationException>(() =>
            provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>());
    }

    [TestMethod]
    public void AddBusinessRuleServices_WithLogConnection_Should_RegisterEfCoreTokenRevocationService()
    {
        var services = new ServiceCollection();

        services.AddBusinessRuleServices(new DatabaseSettings
        {
            ProjectConnectionString = "project-conn",
            LogConnectionString = "log-conn"
        }, new BackgroundQueueSettings());

        var revocationDescriptor = services.LastOrDefault(d => d.ServiceType == typeof(ITokenRevocationService));
        Assert.IsNotNull(revocationDescriptor);
        Assert.AreEqual(ServiceLifetime.Scoped, revocationDescriptor.Lifetime);
        Assert.AreEqual(typeof(EfCoreTokenRevocationService), revocationDescriptor.ImplementationType);

        Assert.IsTrue(services.Any(d => d.ServiceType == typeof(IHostedService) && d.ImplementationType == typeof(QueuedBackgroundService)));
        Assert.IsTrue(services.Any(d => d.ServiceType == typeof(IBackgroundTaskQueue) && d.ImplementationType == typeof(DbBackgroundTaskQueue)));
    }

    [TestMethod]
    public void AddBusinessRuleServices_WithoutLogConnection_Should_RegisterInMemoryTokenRevocationService()
    {
        var services = new ServiceCollection();

        services.AddBusinessRuleServices(new DatabaseSettings
        {
            ProjectConnectionString = "project-conn",
            LogConnectionString = string.Empty
        }, new BackgroundQueueSettings());

        var revocationDescriptor = services.LastOrDefault(d => d.ServiceType == typeof(ITokenRevocationService));
        Assert.IsNotNull(revocationDescriptor);
        Assert.AreEqual(ServiceLifetime.Singleton, revocationDescriptor.Lifetime);
        Assert.AreEqual(typeof(InMemoryTokenRevocationService), revocationDescriptor.ImplementationType);
    }

    [TestMethod]
    public void AddBusinessRuleHealthChecks_Should_ForwardToDataAccessHealthChecks()
    {
        var services = new ServiceCollection();
        var builder = services.AddHealthChecks();

        builder.AddBusinessRuleHealthChecks(new DatabaseSettings
        {
            ProjectConnectionString = "project-conn",
            LogConnectionString = "log-conn"
        });

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value;

        Assert.IsTrue(options.Registrations.Any(r => r.Name == "project-db"));
        Assert.IsTrue(options.Registrations.Any(r => r.Name == "log-db"));
    }
}
