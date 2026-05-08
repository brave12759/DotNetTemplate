using System.ComponentModel;

namespace Template.Common.Enums;

/// <summary>
/// 全域回傳訊息列舉，Status 對應 HTTP 狀態碼，Description 為預設訊息文字
/// </summary>
public enum MessageEnum
{
    // 2xx 成功
    [Description("成功")]
    Success = 200,

    [Description("建立成功")]
    Created = 201,

    [Description("已接受處理")]
    Accepted = 202,

    [Description("無內容")]
    NoContent = 204,

    [Description("重設內容")]
    ResetContent = 205,

    // 4xx 用戶端錯誤
    [Description("請求參數錯誤")]
    BadRequest = 400,

    [Description("未授權，請重新登入")]
    Unauthorized = 401,

    [Description("權限不足")]
    Forbidden = 403,

    [Description("資源不存在")]
    NotFound = 404,

    [Description("資料衝突")]
    Conflict = 409,

    [Description("資料驗證失敗")]
    UnprocessableEntity = 422,

    [Description("請求過於頻繁")]
    TooManyRequests = 429,

    // 4xx 業務錯誤（自訂用於區分特定業務失敗）
    [Description("帳號多次登入失敗已被停用，請聯絡管理員")]
    AccountDisabled = 423,

    // 5xx 伺服器錯誤
    [Description("伺服器發生錯誤")]
    InternalServerError = 500,

    [Description("尚未實作")]
    NotImplemented = 501,

    [Description("閘道錯誤")]
    BadGateway = 502,

    [Description("服務暫時無法使用")]
    ServiceUnavailable = 503,

    [Description("閘道逾時")]
    GatewayTimeout = 504,
}
