namespace Template.Common.Enums;

/// <summary>
/// 系統參數鍵值。
/// </summary>
public enum SystemSettingKeyEnum
{
    /// <summary>
    /// 預設密碼。
    /// </summary>
    DefaultPassword = 1,

    /// <summary>
    /// 登入失敗鎖定門檻。
    /// </summary>
    LoginFailLimit = 2,

    /// <summary>
    /// 帳號鎖定分鐘數。
    /// </summary>
    AccountFailLock = 3,

    /// <summary>
    /// 密碼到期天數。
    /// </summary>
    PasswordExpire = 4
}
