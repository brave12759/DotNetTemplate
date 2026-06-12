namespace Template.BusinessRule.TokenRevocationService.Services;

/// <summary>
/// JWT Token 撤銷服務介面。
/// </summary>
public interface ITokenRevocationService
{
    /// <summary>
    /// 撤銷指定 Token，直到原本的過期時間為止。
    /// </summary>
    void Revoke(string tokenId, long expiredUnixTimeSeconds);

    /// <summary>
    /// 檢查指定 Token 是否已被撤銷。
    /// </summary>
    bool IsRevoked(string tokenId);
}
