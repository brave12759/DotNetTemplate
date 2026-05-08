namespace Template.Common.Settings;

/// <summary>
/// HTTPS 與憑證相關設定。
/// </summary>
public class HttpsSettings
{
    public const string SectionName = "HttpsSettings";

    /// <summary>
    /// 是否強制啟用 HTTPS（啟用後會套用 HSTS 與 HTTPS Redirection）。
    /// </summary>
    public bool EnforceHttps { get; set; } = true;

    /// <summary>
    /// HTTP 轉導到 HTTPS 的狀態碼。預設 307。
    /// </summary>
    public int RedirectStatusCode { get; set; } = 307;

    /// <summary>
    /// 是否啟用 HSTS（僅非 Development 生效）。
    /// </summary>
    public bool HstsEnabled { get; set; } = true;

    /// <summary>
    /// HSTS MaxAge（天）。
    /// </summary>
    public int HstsMaxAgeDays { get; set; } = 180;

    /// <summary>
    /// HSTS 是否包含子網域。
    /// </summary>
    public bool HstsIncludeSubDomains { get; set; } = true;

    /// <summary>
    /// HSTS 是否啟用 Preload。
    /// </summary>
    public bool HstsPreload { get; set; }

    /// <summary>
    /// 伺服器憑證路徑（PFX）。
    /// </summary>
    public string CertificatePath { get; set; } = string.Empty;

    /// <summary>
    /// 伺服器憑證密碼。
    /// </summary>
    public string CertificatePassword { get; set; } = string.Empty;
}