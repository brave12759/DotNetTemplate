namespace Template.WebApi.Models.Auth;

/// <summary>
/// 登入或刷新 Token 成功時回傳給前端的模型。
/// </summary>
public class AuthTokenResponse
{
    /// <summary>
    /// 前端後續呼叫 API 時要放在 Authorization header 的 JWT Token。
    /// </summary>
    public string Token { get; init; } = string.Empty;
}
