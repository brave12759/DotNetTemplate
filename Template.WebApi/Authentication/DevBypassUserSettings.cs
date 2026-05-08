namespace Template.WebApi.Authentication;

/// <summary>
/// Development 環境自動登入假用戶設定，由 appsettings.Development.json 的 "DevBypassUser" 區段載入。
/// </summary>
public class DevBypassUserSettings
{
    public const string SectionName = "DevBypassUser";

    /// <summary>使用者帳號。</summary>
    public string UserId { get; set; } = "dev";

    /// <summary>電子郵件。</summary>
    public string Email { get; set; } = "dev@localhost";

    /// <summary>聯絡電話。</summary>
    public string MobilePhone { get; set; } = string.Empty;

    /// <summary>部門 ID。</summary>
    public string DeptId { get; set; } = "0";
}
