using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Template.BusinessRule.PasswordManager.Services;
using Template.Common.Services;

namespace Template.Test.Tests;

[TestClass]
public class PasswordManagerTests
{
    [TestMethod]
    public void ValidateNewPassword_TooShort_ShouldThrow()
    {
        var sut = BuildPasswordManager();
        Assert.ThrowsException<ArgumentException>(() => sut.ValidateNewPassword("Abc123"));
    }

    [TestMethod]
    public void ValidateNewPassword_NoDigit_ShouldThrow()
    {
        var sut = BuildPasswordManager();
        Assert.ThrowsException<ArgumentException>(() => sut.ValidateNewPassword("Password"));
    }

    [TestMethod]
    public void ValidateNewPassword_NoLetter_ShouldThrow()
    {
        var sut = BuildPasswordManager();
        Assert.ThrowsException<ArgumentException>(() => sut.ValidateNewPassword("12345678"));
    }

    [TestMethod]
    public void HashForStorage_ValidPassword_ShouldReturnHash()
    {
        var sut = BuildPasswordManager();
        var hash = sut.HashForStorage("Abc12345");
        Assert.AreEqual("HASH::Abc12345", hash);
    }

    [TestMethod]
    public void Verify_ValidPair_ShouldReturnTrue()
    {
        var sut = BuildPasswordManager();
        var hash = sut.HashForStorage("Abc12345");
        Assert.IsTrue(sut.Verify("Abc12345", hash));
    }

    [TestMethod]
    public void Verify_InvalidPair_ShouldReturnFalse()
    {
        var sut = BuildPasswordManager();
        var hash = sut.HashForStorage("Abc12345");
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
