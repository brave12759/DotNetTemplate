using Template.Common.Models;

namespace Template.Common.Services;

/// <summary>
/// 提供當前登入使用者資訊的服務介面。
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// 取得當前登入使用者資訊。
    /// </summary>
    CurrentUser CurrentUser { get; }
}
