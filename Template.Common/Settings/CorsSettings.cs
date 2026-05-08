namespace Template.Common.Settings;

/// <summary>
/// CORS 跨域存取設定。
/// </summary>
public class CorsSettings
{
    /// <summary>
    /// appsettings.json 中的區段名稱。
    /// </summary>
    public const string SectionName = "CorsSettings";

    /// <summary>
    /// 允許任何來源（開發環境使用）。
    /// 注意：與 <see cref="AllowCredentials"/> = true 不相容（CORS 安全限制）。
    /// </summary>
    public bool AllowAnyOrigin { get; set; } = false;

    /// <summary>
    /// 允許的來源清單，例如 ["https://example.com", "https://app.example.com"]。
    /// 僅在 <see cref="AllowAnyOrigin"/> = false 時生效。
    /// </summary>
    public string[] AllowedOrigins { get; set; } = [];

    /// <summary>
    /// 是否允許請求攜帶憑證（Cookie、Authorization Header）。
    /// 當 <see cref="AllowAnyOrigin"/> = true 時此設定無效（瀏覽器安全限制）。
    /// </summary>
    public bool AllowCredentials { get; set; } = false;
}
