using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Template.BusinessRule.BackgroundQueue.Services;
using Template.BusinessRule.CryptographyService.Services;
using Template.BusinessRule.DepartmentService.Services;
using Template.BusinessRule.FunctionPermissionService.Services;
using Template.BusinessRule.LoginService.Services;
using Template.BusinessRule.LogService.Services;
using Template.BusinessRule.MenuTreeService.Services;
using Template.BusinessRule.PasswordManager.Services;
using Template.BusinessRule.RoleGroupService.Services;
using Template.BusinessRule.SignalR.Services;
using Template.BusinessRule.SsoService.Services;
using Template.BusinessRule.TokenRevocationService.Services;
using Template.BusinessRule.UserService.Services;
using Template.Common.BackgroundQueue;
using Template.Common.Services;
using Template.Common.Settings;
using Template.Common.SignalR;
using Template.DataAccess.Extensions;
using FunctionPermissionBusinessService = Template.BusinessRule.FunctionPermissionService.Services.FunctionPermissionService;
using DepartmentBusinessService = Template.BusinessRule.DepartmentService.Services.DepartmentService;
using MenuTreeBusinessService = Template.BusinessRule.MenuTreeService.Services.MenuTreeService;
using RoleGroupBusinessService = Template.BusinessRule.RoleGroupService.Services.RoleGroupService;
using LogBusinessService = Template.BusinessRule.LogService.Services.LogService;
using SsoBusinessService = Template.BusinessRule.SsoService.Services.SsoService;

namespace Template.BusinessRule.Extensions;

/// <summary>
/// BusinessRule 層 DI 擴充方法。
/// Program.cs 呼叫此方法即可完成 DataAccess 與所有業務服務的註冊，
/// 不需在 WebApi 直接參考 DataAccess 層。
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 註冊所有業務服務（含 DataAccess DbContext）。
    /// <list type="bullet">
    ///   <item>ProjectDbContext / LogDbContext（轉發至 DataAccess 擴充）</item>
    ///   <item>Token 撤銷服務（LogConnectionString 有值 → EfCore；否則 InMemory）</item>
    ///   <item>CryptographyService / PasswordManager / LoginService / UserService / DepartmentService</item>
    ///   <item>MenuTreeService / RoleGroupService / FunctionPermissionService 為選用服務，預設註解</item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// Token 撤銷：LogConnectionString 為空時自動降級至 InMemory（僅適用單節點）。
    /// 多節點部署請確認 LogConnectionString 已設定。
    /// </remarks>
    public static IServiceCollection AddBusinessRuleServices(
        this IServiceCollection services,
        DatabaseSettings databaseSettings,
        BackgroundQueueSettings backgroundQueueSettings)
    {
        // Infrastructure：DbContext 註冊（定義在 DataAccess 擴充方法）
        services.AddDataAccess(databaseSettings);

        // Token 撤銷策略
        if (!string.IsNullOrWhiteSpace(databaseSettings.LogConnectionString))
            services.AddScoped<ITokenRevocationService, EfCoreTokenRevocationService>();
        else
            services.AddSingleton<ITokenRevocationService, InMemoryTokenRevocationService>();

        // 背景資料庫佇列
        services.AddSingleton(backgroundQueueSettings);
        services.AddScoped<IBackgroundTaskQueue, DbBackgroundTaskQueue>();
        services.AddScoped<IBackgroundJobMonitorService, BackgroundJobMonitorService>();
        services.AddScoped<ISignalRQueueService, SignalRQueueService>();
        services.AddHostedService<QueuedBackgroundService>();

        // 業務服務
        services.AddScoped<ICryptographyService, CryptographyService.Services.CryptographyService>();
        services.AddScoped<IPasswordManager, PasswordManager.Services.PasswordManager>();
        services.AddScoped<ILoginService, LoginService.Services.LoginService>();
        services.AddScoped<ILogWriter, UserOperationLogWriter>();
        services.AddScoped<ILogWriter, QueueLogWriter>();
        services.AddScoped<ILogWriter, SsoLogWriter>();
        services.AddScoped<ILogWriterFactory, LogWriterFactory>();
        services.AddScoped<ILogService, LogBusinessService>();
        services.AddScoped<IUserService, UserService.Services.UserService>();

        // 部門為多數專案共用的基本資料，預設啟用。
        services.AddScoped<IDepartmentService, DepartmentBusinessService>();
        services.AddScoped<ISsoService, SsoBusinessService>();

        // 選用服務：專案需要對應功能時再取消註解，避免未使用模組時仍被迫註冊。
        // services.AddScoped<IMenuTreeService, MenuTreeBusinessService>();
        // services.AddScoped<IRoleGroupService, RoleGroupBusinessService>();
        // services.AddScoped<IFunctionPermissionService, FunctionPermissionBusinessService>();

        return services;
    }

    /// <summary>
    /// 註冊所有業務服務（含 DataAccess DbContext）。未指定背景佇列設定時使用預設值。
    /// </summary>
    public static IServiceCollection AddBusinessRuleServices(
        this IServiceCollection services,
        DatabaseSettings databaseSettings)
    {
        return services.AddBusinessRuleServices(databaseSettings, new BackgroundQueueSettings());
    }

    /// <summary>
    /// 註冊 BusinessRule 所需的健康檢查。
    /// </summary>
    public static IHealthChecksBuilder AddBusinessRuleHealthChecks(
        this IHealthChecksBuilder builder,
        DatabaseSettings databaseSettings)
    {
        return builder.AddDataAccessHealthChecks(databaseSettings);
    }
}
