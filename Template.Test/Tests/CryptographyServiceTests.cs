using System.Security.Cryptography;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Template.BusinessRule.CryptographyService.Services;
using Template.Common.Settings;

namespace Template.Test.Tests;

[TestClass]
public class CryptographyServiceTests
{
    private static CryptographyService _sut = null!;
    private static CryptographyKeySettings _keySettings = null!;
    private static HashSettings _hashSettings = null!;

    [ClassInitialize]
    public static void ClassInit(TestContext _)
    {
        // 產生 RSA 測試金鑰對（一次即可，整個測試類別共用）
        using var rsa = RSA.Create(2048);
        var publicPem = rsa.ExportRSAPublicKeyPem();
        var privatePem = rsa.ExportRSAPrivateKeyPem();

        // 產生 AES 256-bit 測試金鑰
        var aesKey = RandomNumberGenerator.GetBytes(32);
        var aesIv = RandomNumberGenerator.GetBytes(16);

        _keySettings = new CryptographyKeySettings
        {
            SymmetricKeyBase64 = Convert.ToBase64String(aesKey),
            SymmetricIvBase64 = Convert.ToBase64String(aesIv),
            RsaPublicKeyPem = publicPem,
            RsaPrivateKeyPem = privatePem
        };

        _hashSettings = new HashSettings { Iterations = 10000 };

        _sut = new CryptographyService(_keySettings, _hashSettings);
    }

    // ── AES 對稱加解密 ──────────────────────────────────────────────

    [TestMethod]
    public void SymmetricEncrypt_Decrypt_RoundTrip()
    {
        const string original = "Hello 世界 123!";
        var cipher = _sut.SymmetricEncrypt(original);
        var plain = _sut.SymmetricDecrypt(cipher);
        Assert.AreEqual(original, plain);
    }

    [TestMethod]
    public void SymmetricEncrypt_SameInput_DifferentInstances_DifferentIvProducesDifferentCipher()
    {
        // 使用兩組不同金鑰的 sut，相同明文加密結果應不同
        var altKey = RandomNumberGenerator.GetBytes(32);
        var altIv = RandomNumberGenerator.GetBytes(16);
        var altSettings = new CryptographyKeySettings
        {
            SymmetricKeyBase64 = Convert.ToBase64String(altKey),
            SymmetricIvBase64 = Convert.ToBase64String(altIv),
            RsaPublicKeyPem = _keySettings.RsaPublicKeyPem,
            RsaPrivateKeyPem = _keySettings.RsaPrivateKeyPem
        };
        var altSut = new CryptographyService(altSettings, _hashSettings);

        var cipher1 = _sut.SymmetricEncrypt("test");
        var cipher2 = altSut.SymmetricEncrypt("test");
        Assert.AreNotEqual(cipher1, cipher2);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void SymmetricEncrypt_EmptyText_ThrowsArgumentException()
    {
        _sut.SymmetricEncrypt(" ");
    }

    [TestMethod]
    [ExpectedException(typeof(CryptographicException))]
    public void SymmetricDecrypt_WrongKey_ThrowsCryptographicException()
    {
        var cipher = _sut.SymmetricEncrypt("secret");

        var wrongKey = RandomNumberGenerator.GetBytes(32);
        var wrongIv = RandomNumberGenerator.GetBytes(16);
        var wrongSettings = new CryptographyKeySettings
        {
            SymmetricKeyBase64 = Convert.ToBase64String(wrongKey),
            SymmetricIvBase64 = Convert.ToBase64String(wrongIv),
            RsaPublicKeyPem = _keySettings.RsaPublicKeyPem,
            RsaPrivateKeyPem = _keySettings.RsaPrivateKeyPem
        };
        var wrongSut = new CryptographyService(wrongSettings, _hashSettings);
        wrongSut.SymmetricDecrypt(cipher);
    }

    // ── RSA 非對稱加解密 ────────────────────────────────────────────

    [TestMethod]
    public void AsymmetricEncrypt_Decrypt_RoundTrip()
    {
        const string original = "機密資料 ABC";
        var cipher = _sut.AsymmetricEncrypt(original);
        var plain = _sut.AsymmetricDecrypt(cipher);
        Assert.AreEqual(original, plain);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void AsymmetricEncrypt_EmptyText_ThrowsArgumentException()
    {
        _sut.AsymmetricEncrypt("");
    }

    // ── RSA 簽章與驗證 ──────────────────────────────────────────────

    [TestMethod]
    public void Sign_VerifySignature_RoundTrip()
    {
        const string message = "待簽署訊息";
        var sig = _sut.Sign(message);
        Assert.IsTrue(_sut.VerifySignature(message, sig));
    }

    [TestMethod]
    public void VerifySignature_TamperedData_ReturnsFalse()
    {
        var sig = _sut.Sign("original");
        Assert.IsFalse(_sut.VerifySignature("tampered", sig));
    }

    // ── PBKDF2 雜湊 ────────────────────────────────────────────────

    [TestMethod]
    public void Hash_VerifyHash_RoundTrip()
    {
        const string password = "P@ssw0rd!";
        var hashValue = _sut.Hash(password);
        Assert.IsTrue(_sut.VerifyHash(password, hashValue));
    }

    [TestMethod]
    public void VerifyHash_WrongPassword_ReturnsFalse()
    {
        var hashValue = _sut.Hash("correct");
        Assert.IsFalse(_sut.VerifyHash("wrong", hashValue));
    }

    [TestMethod]
    public void Hash_TwoCalls_SamePlainText_DifferentResults()
    {
        // 每次 Hash 使用隨機 salt，兩次結果不同
        var h1 = _sut.Hash("abc");
        var h2 = _sut.Hash("abc");
        Assert.AreNotEqual(h1, h2);
    }

    [TestMethod]
    [ExpectedException(typeof(FormatException))]
    public void VerifyHash_InvalidFormat_ThrowsFormatException()
    {
        _sut.VerifyHash("password", "bad-format");
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void Hash_EmptyText_ThrowsArgumentException()
    {
        _sut.Hash(" ");
    }

    // ── 金鑰產生工具 ────────────────────────────────────────────────

    [TestMethod]
    public void GenerateSymmetricKey_256bit_ReturnsValidBase64()
    {
        var (key, iv) = _sut.GenerateSymmetricKey(256);
        var keyBytes = Convert.FromBase64String(key);
        var ivBytes = Convert.FromBase64String(iv);
        Assert.AreEqual(32, keyBytes.Length);
        Assert.AreEqual(16, ivBytes.Length);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentOutOfRangeException))]
    public void GenerateSymmetricKey_InvalidSize_ThrowsArgumentOutOfRangeException()
    {
        _sut.GenerateSymmetricKey(100);
    }

    [TestMethod]
    public void GenerateRsaKeyPair_ReturnsPemStrings()
    {
        var (pub, priv) = _sut.GenerateRsaKeyPair(2048);
        StringAssert.StartsWith(pub, "-----BEGIN RSA PUBLIC KEY-----");
        StringAssert.StartsWith(priv, "-----BEGIN RSA PRIVATE KEY-----");
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentOutOfRangeException))]
    public void GenerateRsaKeyPair_TooShort_ThrowsArgumentOutOfRangeException()
    {
        _sut.GenerateRsaKeyPair(1024);
    }
}
