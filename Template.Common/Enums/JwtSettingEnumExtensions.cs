namespace Template.Common.Enums;

/// <summary>
/// JWT 設定列舉轉資料庫字串。
/// </summary>
public static class JwtSettingEnumExtensions
{
    public static string ToSettingTypeValue(this JwtSettingTypeEnum type)
    {
        return type switch
        {
            JwtSettingTypeEnum.JwtSetting => "JwtSetting",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unsupported setting type.")
        };
    }

    public static string ToSettingKeyValue(this JwtSettingKeyEnum key)
    {
        return key switch
        {
            JwtSettingKeyEnum.SecretKey => "SecretKey",
            JwtSettingKeyEnum.Issuer => "Issuer",
            JwtSettingKeyEnum.Audience => "Audience",
            JwtSettingKeyEnum.PersonalTokenExpire => "PersonalTokenExpire",
            JwtSettingKeyEnum.ServerTokenExpire => "ServerTokenExpire",
            _ => throw new ArgumentOutOfRangeException(nameof(key), key, "Unsupported setting key.")
        };
    }
}
