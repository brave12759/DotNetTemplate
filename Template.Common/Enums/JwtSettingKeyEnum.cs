namespace Template.Common.Enums;

/// <summary>
/// JWT 設定鍵值。
/// </summary>
public enum JwtSettingKeyEnum
{
    SecretKey = 1,
    Issuer = 2,
    Audience = 3,
    PersonalTokenExpire = 4,
    ServerTokenExpire = 5
}
