using System.ComponentModel;

namespace Template.BusinessRule.SsoService.Enums;

/// <summary>
/// SSO API 回傳與服務層驗證使用的訊息代碼。
/// </summary>
public enum SsoMessageEnum
{
    /// <summary>
    /// 資料識別碼必須大於零。
    /// </summary>
    [Description("Id 必須大於 0。")]
    IdMustBeGreaterThanZero = 1,

    /// <summary>
    /// ClientId 未填寫。
    /// </summary>
    [Description("ClientId 不可為空。")]
    ClientIdRequired = 2,

    /// <summary>
    /// ClientName 未填寫。
    /// </summary>
    [Description("ClientName 不可為空。")]
    ClientNameRequired = 3,

    /// <summary>
    /// ClientSecret 未填寫。
    /// </summary>
    [Description("ClientSecret 不可為空。")]
    ClientSecretRequired = 4,

    /// <summary>
    /// ClientId 已經存在。
    /// </summary>
    [Description("ClientId 已存在。")]
    ClientIdAlreadyExists = 5,

    /// <summary>
    /// 找不到指定的 SSO client。
    /// </summary>
    [Description("SSO client 不存在。")]
    ClientNotFound = 6,

    /// <summary>
    /// ClientId 或 ClientSecret 驗證失敗。
    /// </summary>
    [Description("ClientId 或 ClientSecret 錯誤。")]
    InvalidClientCredentials = 7,

    /// <summary>
    /// 更新 SSO client 成功。
    /// </summary>
    [Description("更新成功。")]
    UpdatedSuccessfully = 8,

    /// <summary>
    /// 刪除 SSO client 成功。
    /// </summary>
    [Description("刪除成功。")]
    DeletedSuccessfully = 9
}
