using Microsoft.Extensions.DependencyInjection;
using Template.Common.Models;
using Template.Common.Services;
using Template.DataAccess.ProjectDbContext;
using Template.DataAccess.LogDbContext;

namespace Template.BusinessRule;

/// <summary>
/// 邏輯層基底服務，透過 Lazy 延遲載入提供常用相依性。
/// </summary>
public abstract class BaseService
{
    private readonly IServiceProvider _serviceProvider;

    private readonly Lazy<ProjectDbContext> _db;
    private readonly Lazy<LogDbContext> _logDb;
    private readonly Lazy<ICurrentUserService> _currentUserService;

    /// <summary>
    /// 建立基底服務。
    /// </summary>
    /// <param name="serviceProvider">DI 服務提供者。</param>
    protected BaseService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;

        _db                 = new Lazy<ProjectDbContext>(() => _serviceProvider.GetRequiredService<ProjectDbContext>());
        _logDb              = new Lazy<LogDbContext>(() => _serviceProvider.GetRequiredService<LogDbContext>());
        _currentUserService = new Lazy<ICurrentUserService>(() => _serviceProvider.GetRequiredService<ICurrentUserService>());
    }

    /// <summary>
    /// 主資料庫（ProjectDbContext）。
    /// 用法：Db.Sys_UserInfos.AsNoTracking()...
    /// </summary>
    protected ProjectDbContext Db => _db.Value;

    /// <summary>
    /// 日誌資料庫（LogDbContext）。
    /// </summary>
    protected LogDbContext LogDb => _logDb.Value;

    /// <summary>
    /// 當前登入使用者資訊（由 JWT 解析）。
    /// </summary>
    protected ICurrentUserService CurrentUserService => _currentUserService.Value;

    /// <summary>
    /// 當前登入使用者。
    /// </summary>
    protected CurrentUser CurrentUser => CurrentUserService.CurrentUser;
}
