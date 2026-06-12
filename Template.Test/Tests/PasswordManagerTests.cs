using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Template.BusinessRule.CryptographyService.Services;
using Template.BusinessRule.PasswordManager.Services;

namespace Template.Test.Tests;

[TestClass]
public class PasswordManagerTests
{
    [TestMethod]
    public void ValidateNewPassword_ValidComplexTwelveCharacters_ShouldPass()
    {
        var sut = BuildPasswordManager();
        sut.ValidateNewPassword("Aa123456789!");
    }

    [TestMethod]
    public void ValidateNewPassword_TooShort_ShouldThrow()
    {
        var sut = BuildPasswordManager();
        Assert.ThrowsException<ArgumentException>(() => sut.ValidateNewPassword("Abc123!"));
    }

    [TestMethod]
    public void ValidateNewPassword_NoDigit_ShouldThrow()
    {
        var sut = BuildPasswordManager();
        Assert.ThrowsException<ArgumentException>(() => sut.ValidateNewPassword("PasswordOnly!"));
    }

    [TestMethod]
    public void ValidateNewPassword_NoLetter_ShouldThrow()
    {
        var sut = BuildPasswordManager();
        Assert.ThrowsException<ArgumentException>(() => sut.ValidateNewPassword("1234567890!@"));
    }

    [TestMethod]
    public void ValidateNewPassword_NoUppercase_ShouldThrow()
    {
        var sut = BuildPasswordManager();
        Assert.ThrowsException<ArgumentException>(() => sut.ValidateNewPassword("lowercase@123"));
    }

    [TestMethod]
    public void ValidateNewPassword_NoLowercase_ShouldThrow()
    {
        var sut = BuildPasswordManager();
        Assert.ThrowsException<ArgumentException>(() => sut.ValidateNewPassword("UPPERCASE@123"));
    }

    [TestMethod]
    public void ValidateNewPassword_NoSymbol_ShouldThrow()
    {
        var sut = BuildPasswordManager();
        Assert.ThrowsException<ArgumentException>(() => sut.ValidateNewPassword("Password1234"));
    }

    [TestMethod]
    public void HashForStorage_ValidPassword_ShouldReturnHash()
    {
        var sut = BuildPasswordManager();
        var hash = sut.HashForStorage("ValidPass@123");
        Assert.AreEqual("HASH::ValidPass@123", hash);
    }

    [TestMethod]
    public void Verify_ValidPair_ShouldReturnTrue()
    {
        var sut = BuildPasswordManager();
        var hash = sut.HashForStorage("ValidPass@123");
        Assert.IsTrue(sut.Verify("ValidPass@123", hash));
    }

    [TestMethod]
    public void Verify_InvalidPair_ShouldReturnFalse()
    {
        var sut = BuildPasswordManager();
        var hash = sut.HashForStorage("ValidPass@123");
        Assert.IsFalse(sut.Verify("Wrong999", hash));
    }

    private static IPasswordManager BuildPasswordManager()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ICryptographyService, FakeCryptographyService>();

        var provider = services.BuildServiceProvider();
        return new PasswordManager(provider);
    }

    private sealed class FakeCryptographyService : ICryptographyService
    {
        public (string KeyBase64, string IvBase64) GenerateSymmetricKey(int keySizeBits = 256) => throw new NotImplementedException();
        public string SymmetricEncrypt(string plainText) => throw new NotImplementedException();
        public string SymmetricDecrypt(string cipherTextBase64) => throw new NotImplementedException();
        public (string PublicKeyPem, string PrivateKeyPem) GenerateRsaKeyPair(int keySizeBits = 2048) => throw new NotImplementedException();
        public string AsymmetricEncrypt(string plainText) => throw new NotImplementedException();
        public string AsymmetricDecrypt(string cipherTextBase64) => throw new NotImplementedException();
        public string Sign(string plainText) => throw new NotImplementedException();
        public bool VerifySignature(string plainText, string signatureBase64) => throw new NotImplementedException();
        public string Hash(string plainText) => $"HASH::{plainText}";
        public bool VerifyHash(string plainText, string hashValue) => hashValue == Hash(plainText);
    }
}
