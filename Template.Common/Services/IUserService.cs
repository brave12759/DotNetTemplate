using Template.Common.Models.User;

namespace Template.Common.Services;

/// <summary>
/// 使用者管理服務介面。
/// </summary>
public interface IUserService
{
    /// <summary>
    /// 取得使用者清單（可依關鍵字與啟用狀態篩選）。
    /// </summary>
    Task<IReadOnlyList<UserDto>> GetListAsync(string? keyword, bool? isEnable);

    /// <summary>
    /// 依主鍵取得使用者。
    /// </summary>
    Task<UserDto?> GetByIdAsync(int id);

    /// <summary>
    /// 建立使用者。
    /// </summary>
    Task<UserDto> CreateAsync(UserCreateRequest request);

    /// <summary>
    /// 更新使用者基本資料（不含密碼）。
    /// </summary>
    Task<bool> UpdateAsync(UserUpdateRequest request);

    /// <summary>
    /// 刪除使用者。
    /// </summary>
    Task<bool> DeleteAsync(int id);

    /// <summary>
    /// 重設使用者密碼。
    /// </summary>
    Task<bool> ResetPasswordAsync(UserResetPasswordRequest request);

    /// <summary>
    /// 修改使用者密碼（需驗證舊密碼）。
    /// </summary>
    Task<bool> ChangePasswordAsync(UserChangePasswordRequest request);
}
