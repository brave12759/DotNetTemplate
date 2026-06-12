namespace Template.Common.Enums;

/// <summary>
/// 系統參數列舉轉資料庫鍵值字串。
/// </summary>
public static class SystemSettingEnumExtensions
{
    public static string ToSettingTypeValue(this SystemSettingTypeEnum type)
    {
        return type switch
        {
            SystemSettingTypeEnum.SystemSetting => "SystemSetting",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unsupported setting type.")
        };
    }

    public static string ToSettingKeyValue(this SystemSettingKeyEnum key)
    {
        return key switch
        {
            SystemSettingKeyEnum.DefaultPassword => "DefaultPassword",
            SystemSettingKeyEnum.LoginFailLimit => "LoginFailLimit",
            SystemSettingKeyEnum.AccountFailLock => "AccountFailLock",
            SystemSettingKeyEnum.PasswordExpire => "PassWordExpire",
            _ => throw new ArgumentOutOfRangeException(nameof(key), key, "Unsupported setting key.")
        };
    }
}
