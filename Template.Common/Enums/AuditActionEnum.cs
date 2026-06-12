namespace Template.Common.Enums;

/// <summary>
/// 稽核日誌常見操作種類。
/// </summary>
public enum AuditActionEnum
{
    /// <summary>
    /// 新增資料。
    /// </summary>
    Create = 1,

    /// <summary>
    /// 更新資料。
    /// </summary>
    Update = 2,

    /// <summary>
    /// 刪除資料。
    /// </summary>
    Delete = 3,

    /// <summary>
    /// 登入。
    /// </summary>
    Login = 4,

    /// <summary>
    /// 登出。
    /// </summary>
    Logout = 5,

    /// <summary>
    /// 重新整理 Token。
    /// </summary>
    RefreshToken = 6,

    /// <summary>
    /// 重設密碼。
    /// </summary>
    PasswordReset = 7,

    /// <summary>
    /// 使用者自行變更密碼。
    /// </summary>
    PasswordChange = 8,

    /// <summary>
    /// 權限異動。
    /// </summary>
    PermissionChange = 9,

    /// <summary>
    /// 匯入資料。
    /// </summary>
    Import = 10,

    /// <summary>
    /// 匯出資料。
    /// </summary>
    Export = 11,

    /// <summary>
    /// 系統執行或背景作業。
    /// </summary>
    Execute = 12
}
