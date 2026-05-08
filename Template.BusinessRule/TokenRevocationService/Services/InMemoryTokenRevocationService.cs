using System.Collections.Concurrent;
using Template.Common.Services;

namespace Template.BusinessRule.TokenRevocationService.Services;

/// <summary>
/// In-Memory JWT 撤銷服務（單機 / 開發環境）。
/// 以 jti 作為索引，儲存到期時間；到期後會在查詢時清理。
/// </summary>
public class InMemoryTokenRevocationService : ITokenRevocationService
{
    private readonly ConcurrentDictionary<string, DateTimeOffset> _revokedTokens = new();

    /// <inheritdoc />
    public void Revoke(string tokenId, long expiredUnixTimeSeconds)
    {
        if (string.IsNullOrWhiteSpace(tokenId) || expiredUnixTimeSeconds <= 0)
            return;

        var expiresAt = DateTimeOffset.FromUnixTimeSeconds(expiredUnixTimeSeconds);
        if (expiresAt <= DateTimeOffset.UtcNow)
            return;

        _revokedTokens[tokenId] = expiresAt;
    }

    /// <inheritdoc />
    public bool IsRevoked(string tokenId)
    {
        if (string.IsNullOrWhiteSpace(tokenId))
            return false;

        if (!_revokedTokens.TryGetValue(tokenId, out var expiresAt))
            return false;

        if (expiresAt <= DateTimeOffset.UtcNow)
        {
            _revokedTokens.TryRemove(tokenId, out _);
            return false;
        }

        return true;
    }
}
