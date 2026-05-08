using Microsoft.VisualStudio.TestTools.UnitTesting;
using Template.BusinessRule.TokenRevocationService.Services;

namespace Template.Test.Tests;

[TestClass]
public class InMemoryTokenRevocationServiceTests
{
    private InMemoryTokenRevocationService _sut = null!;

    [TestInitialize]
    public void Init() => _sut = new InMemoryTokenRevocationService();

    // ── IsRevoked 基本情境 ──────────────────────────────────────────

    [TestMethod]
    public void IsRevoked_UnknownToken_ReturnsFalse()
    {
        Assert.IsFalse(_sut.IsRevoked("unknown-jti"));
    }

    [TestMethod]
    public void IsRevoked_NullOrEmpty_ReturnsFalse()
    {
        Assert.IsFalse(_sut.IsRevoked(null!));
        Assert.IsFalse(_sut.IsRevoked(""));
        Assert.IsFalse(_sut.IsRevoked("   "));
    }

    // ── Revoke + IsRevoked ──────────────────────────────────────────

    [TestMethod]
    public void Revoke_ValidToken_IsRevoked_ReturnsTrue()
    {
        var jti = Guid.NewGuid().ToString();
        var futureExp = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds();

        _sut.Revoke(jti, futureExp);

        Assert.IsTrue(_sut.IsRevoked(jti));
    }

    [TestMethod]
    public void Revoke_AlreadyExpired_IsRevoked_ReturnsFalse()
    {
        var jti = Guid.NewGuid().ToString();
        var pastExp = DateTimeOffset.UtcNow.AddSeconds(-1).ToUnixTimeSeconds();

        _sut.Revoke(jti, pastExp);   // 已過期，不應存入

        Assert.IsFalse(_sut.IsRevoked(jti));
    }

    [TestMethod]
    public void Revoke_NullOrEmpty_NoException_NoEntry()
    {
        // 不應拋例外，也不應產生假資料
        var futureExp = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds();
        _sut.Revoke(null!, futureExp);
        _sut.Revoke("", futureExp);
        _sut.Revoke("   ", futureExp);

        Assert.IsFalse(_sut.IsRevoked(null!));
        Assert.IsFalse(_sut.IsRevoked(""));
    }

    [TestMethod]
    public void Revoke_ZeroOrNegativeExp_Ignored()
    {
        var jti = Guid.NewGuid().ToString();
        _sut.Revoke(jti, 0);
        _sut.Revoke(jti, -100);
        Assert.IsFalse(_sut.IsRevoked(jti));
    }

    // ── 自動清理過期 Token ──────────────────────────────────────────

    [TestMethod]
    public void IsRevoked_ExpiredEntry_AutoCleaned_ReturnsFalse()
    {
        var jti = Guid.NewGuid().ToString();
        // 設定 1 秒後到期的時間（Unix timestamp）
        var nearFutureExp = DateTimeOffset.UtcNow.AddSeconds(1).ToUnixTimeSeconds();
        _sut.Revoke(jti, nearFutureExp);

        Assert.IsTrue(_sut.IsRevoked(jti), "Token 應在到期前視為已撤銷");

        // 等待超過到期時間
        var deadline = DateTime.UtcNow.AddSeconds(2);
        while (DateTime.UtcNow < deadline) { }

        Assert.IsFalse(_sut.IsRevoked(jti), "Token 到期後應自動清理並回傳 false");
    }

    // ── 多 Token 互不干擾 ───────────────────────────────────────────

    [TestMethod]
    public void Revoke_MultipleTokens_AreIndependent()
    {
        var jtiA = Guid.NewGuid().ToString();
        var jtiB = Guid.NewGuid().ToString();
        var futureExp = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds();

        _sut.Revoke(jtiA, futureExp);

        Assert.IsTrue(_sut.IsRevoked(jtiA));
        Assert.IsFalse(_sut.IsRevoked(jtiB));
    }
}
