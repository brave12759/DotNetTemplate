namespace Template.Common.Settings;

public class JwtSettings
{
    public const string SectionName = "JwtSettings";

    /// <summary>
    /// 簽章金鑰（至少 32 字元）
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// 核發者
    /// </summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// 受眾
    /// </summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Token 有效分鐘數（預設 60 分鐘）
    /// </summary>
    public int ExpiresMinutes { get; set; } = 60;
}
