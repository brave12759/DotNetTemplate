using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Template.Common.Settings;
using ProjectDb = Template.DataAccess.ProjectDbContext.ProjectDbContext;
using LogDb = Template.DataAccess.LogDbContext.LogDbContext;

namespace Template.DataAccess.Extensions;

/// <summary>
/// DataAccess 層 DI 擴充方法。
/// 由 BusinessRule 層呼叫，WebApi 不直接參考 DataAccess。
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 註冊 ProjectDbContext 與 LogDbContext。
    /// </summary>
    public static IServiceCollection AddDataAccess(
        this IServiceCollection services,
        DatabaseSettings settings)
    {
        services.AddDbContext<ProjectDb>(options =>
            options.UseSqlServer(settings.ProjectConnectionString));

        services.AddDbContext<LogDb>(options =>
            options.UseSqlServer(settings.LogConnectionString));

        return services;
    }

    /// <summary>
    /// 加入 DbContext 健康檢查（連線字串有設定才加入，本機開發空字串時自動略過）。
    /// </summary>
    public static IHealthChecksBuilder AddDataAccessHealthChecks(
        this IHealthChecksBuilder builder,
        DatabaseSettings settings)
    {
        if (!string.IsNullOrWhiteSpace(settings.ProjectConnectionString))
            builder.AddDbContextCheck<ProjectDb>("project-db", tags: ["db", "ready"]);

        if (!string.IsNullOrWhiteSpace(settings.LogConnectionString))
            builder.AddDbContextCheck<LogDb>("log-db", tags: ["db", "ready"]);

        return builder;
    }
}
