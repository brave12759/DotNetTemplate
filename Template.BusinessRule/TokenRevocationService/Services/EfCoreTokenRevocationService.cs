using Microsoft.EntityFrameworkCore;
using Template.Common.Services;
using Template.DataAccess.LogDbContext;

namespace Template.BusinessRule.TokenRevocationService.Services;

/// <summary>
/// EF Core（SQL Server）JWT 撤銷服務（多節點共用）。
/// 以 jti 為主鍵，透過 LINQ 操作 LogDbContext.TokenRevocations，
/// 所有 API 節點連同一資料庫即可共享登出狀態。
/// </summary>
public class EfCoreTokenRevocationService(LogDbContext logDb) : ITokenRevocationService
{
    private static bool _initialized;
    private static readonly Lock _initLock = new();

    /// <inheritdoc />
    public void Revoke(string tokenId, long expiredUnixTimeSeconds)
    {
        if (string.IsNullOrWhiteSpace(tokenId) || expiredUnixTimeSeconds <= 0)
            return;

        EnsureTable();

        var expiresAt = DateTimeOffset.FromUnixTimeSeconds(expiredUnixTimeSeconds).UtcDateTime;
        if (expiresAt <= DateTime.UtcNow)
            return;

        var existing = logDb.TokenRevocations.Find(tokenId);
        if (existing is null)
        {
            logDb.TokenRevocations.Add(new TokenRevocation
            {
                TokenId     = tokenId,
                ExpiresUtc  = expiresAt,
                RevokedTime = DateTime.UtcNow
            });
        }
        else
        {
            existing.ExpiresUtc  = expiresAt;
            existing.RevokedTime = DateTime.UtcNow;
        }

        logDb.SaveChanges();

        // 順便清理已過期的撤銷紀錄
        var expired = logDb.TokenRevocations
            .Where(t => t.ExpiresUtc <= DateTime.UtcNow)
            .ToList();

        if (expired.Count > 0)
        {
            logDb.TokenRevocations.RemoveRange(expired);
            logDb.SaveChanges();
        }
    }

    /// <inheritdoc />
    public bool IsRevoked(string tokenId)
    {
        if (string.IsNullOrWhiteSpace(tokenId))
            return false;

        EnsureTable();

        var record = logDb.TokenRevocations.FirstOrDefault(t => t.TokenId == tokenId);
        if (record is null)
            return false;

        if (record.ExpiresUtc <= DateTime.UtcNow)
        {
            logDb.TokenRevocations.Remove(record);
            logDb.SaveChanges();
            return false;
        }

        return true;
    }

    /// <summary>
    /// 確保 TokenRevocation 資料表已建立（首次啟動時自動執行 EnsureCreated）。
    /// </summary>
    private void EnsureTable()
    {
        if (_initialized)
            return;

        lock (_initLock)
        {
            if (_initialized)
                return;

            logDb.Database.EnsureCreated();
            _initialized = true;
        }
    }
}
