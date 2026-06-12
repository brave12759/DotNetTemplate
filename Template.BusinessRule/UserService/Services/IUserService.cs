using Template.Common.Models;
using Template.Common.Models.User;

namespace Template.BusinessRule.UserService.Services;

/// <summary>
/// 使用者資料維護服務介面。
/// </summary>
public interface IUserService
{
    /// <summary>
    /// 取得使用者清單，可依關鍵字、啟用狀態、部門與子部門篩選。
    /// </summary>
    /// <param name="keyword">關鍵字，會比對帳號、姓名、Email、手機、部門 ID 與部門名稱。</param>
    /// <param name="isEnable">啟用狀態；空值代表不篩選。</param>
    /// <param name="deptId">部門 ID；空值代表不篩選部門。</param>
    /// <param name="includeSubDepartments">是否包含指定部門底下所有子部門。</param>
    Task<PageListOutput<UserDto>> GetListAsync(
        string? keyword,
        bool? isEnable,
        int? deptId = null,
        bool includeSubDepartments = false,
        bool enablePaging = false,
        int page = 1,
        int pageSize = 50);

    /// <summary>
    /// 依使用者流水號取得單筆使用者。
    /// </summary>
    Task<UserDto?> GetByIdAsync(int id);

    /// <summary>
    /// 建立使用者。
    /// </summary>
    Task<UserDto> CreateAsync(UserCreateRequest request);

    /// <summary>
    /// 更新使用者基本資料。
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
    /// 變更使用者密碼。
    /// </summary>
    Task<bool> ChangePasswordAsync(UserChangePasswordRequest request);
}
