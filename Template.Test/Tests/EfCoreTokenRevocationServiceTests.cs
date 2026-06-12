using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Template.BusinessRule.TokenRevocationService.Services;
using Template.DataAccess.LogDbContext;

namespace Template.Test.Tests;

[TestClass]
public class EfCoreTokenRevocationServiceTests
{
    [TestInitialize]
    public void ResetInitializedFlag()
    {
        var field = typeof(EfCoreTokenRevocationService).GetField("_initialized", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new AssertFailedException("_initialized field not found.");
        field.SetValue(null, false);
    }

    [TestMethod]
    public void Revoke_InvalidTokenOrExpired_Should_DoNothing()
    {
        using var db = CreateDb();
        var sut = new EfCoreTokenRevocationService(db);

        sut.Revoke("", DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds());
        sut.Revoke("token-a", 0);
        sut.Revoke("token-b", DateTimeOffset.UtcNow.AddMinutes(-1).ToUnixTimeSeconds());

        Assert.AreEqual(0, db.TokenRevocations.Count());
    }

    [TestMethod]
    public void Revoke_ValidToken_Should_CreateRevocationRecord()
    {
        using var db = CreateDb();
        var sut = new EfCoreTokenRevocationService(db);
        var exp = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds();

        sut.Revoke("jti-1", exp);

        var row = db.TokenRevocations.SingleOrDefault(x => x.TokenId == "jti-1");
        Assert.IsNotNull(row);
        Assert.IsTrue(row.ExpiresUtc > DateTime.UtcNow);
        Assert.IsTrue(sut.IsRevoked("jti-1"));
    }

    [TestMethod]
    public void Revoke_ExistingToken_Should_UpdateInsteadOfInsert()
    {
        using var db = CreateDb();
        var sut = new EfCoreTokenRevocationService(db);
        var exp1 = DateTimeOffset.UtcNow.AddMinutes(30).ToUnixTimeSeconds();
        var exp2 = DateTimeOffset.UtcNow.AddHours(2).ToUnixTimeSeconds();

        sut.Revoke("jti-2", exp1);
        sut.Revoke("jti-2", exp2);

        var rows = db.TokenRevocations.Where(x => x.TokenId == "jti-2").ToList();
        Assert.AreEqual(1, rows.Count);
        Assert.IsTrue(rows[0].ExpiresUtc > DateTime.UtcNow.AddHours(1));
    }

    [TestMethod]
    public void IsRevoked_ExpiredRecord_Should_RemoveAndReturnFalse()
    {
        using var db = CreateDb();
        db.TokenRevocations.Add(new TokenRevocation
        {
            TokenId = "expired-jti",
            ExpiresUtc = DateTime.UtcNow.AddSeconds(-5),
            RevokedTime = DateTime.UtcNow.AddMinutes(-1)
        });
        db.SaveChanges();

        var sut = new EfCoreTokenRevocationService(db);

        var isRevoked = sut.IsRevoked("expired-jti");

        Assert.IsFalse(isRevoked);
        Assert.IsFalse(db.TokenRevocations.Any(x => x.TokenId == "expired-jti"));
    }

    [TestMethod]
    public void Revoke_Should_PurgePreviouslyExpiredRecords()
    {
        using var db = CreateDb();
        db.TokenRevocations.Add(new TokenRevocation
        {
            TokenId = "old-expired",
            ExpiresUtc = DateTime.UtcNow.AddSeconds(-1),
            RevokedTime = DateTime.UtcNow.AddMinutes(-2)
        });
        db.SaveChanges();

        var sut = new EfCoreTokenRevocationService(db);
        var exp = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds();

        sut.Revoke("new-token", exp);

        Assert.IsFalse(db.TokenRevocations.Any(x => x.TokenId == "old-expired"));
        Assert.IsTrue(db.TokenRevocations.Any(x => x.TokenId == "new-token"));
    }

    private static LogDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<LogDbContext>()
            .UseInMemoryDatabase($"token-revoke-tests-{Guid.NewGuid():N}")
            .Options;

        return new LogDbContext(options);
    }
}
