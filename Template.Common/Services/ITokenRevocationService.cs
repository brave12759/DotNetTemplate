namespace Template.Common.Services;

/// <summary>
/// JWT Token 撤銷服務。
/// </summary>
public interface ITokenRevocationService
{
    /// <summary>
    /// 將指定 Token（以 jti 辨識）加入撤銷清單，直到其到期時間為止。
    /// </summary>
    /// <param name="tokenId">JWT jti。</param>
    /// <param name="expiredUnixTimeSeconds">JWT exp（Unix Timestamp）。</param>
    void Revoke(string tokenId, long expiredUnixTimeSeconds);

    /// <summary>
    /// 檢查指定 Token 是否已撤銷。
    /// </summary>
    /// <param name="tokenId">JWT jti。</param>
    /// <returns>若已撤銷回傳 true。</returns>
    bool IsRevoked(string tokenId);
}
