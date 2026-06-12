using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Template.BusinessRule.CryptographyService.Models;
using Template.BusinessRule.CryptographyService.Services;
using Template.WebApi.Controllers;

namespace Template.Test.Tests;

[TestClass]
public class CryptographyControllerAdditionalTests
{
    [TestMethod]
    public void SymmetricEncrypt_ArgumentException_Should_ReturnBadRequest()
    {
        var controller = CreateController(new FlexibleCryptographyService
        {
            SymmetricEncryptFunc = _ => throw new ArgumentException("bad")
        });

        var result = controller.SymmetricEncrypt(new CryptographyServiceSymmetricEncryptRequest { PlainText = "hello" });

        Assert.IsInstanceOfType<BadRequestObjectResult>(result);
    }

    [TestMethod]
    public void SymmetricDecrypt_Success_Should_ReturnOk()
    {
        var controller = CreateController(new FlexibleCryptographyService
        {
            SymmetricDecryptFunc = _ => "plain-text"
        });

        var result = controller.SymmetricDecrypt(new CryptographyServiceSymmetricDecryptRequest { CipherTextBase64 = "abc" });

        var ok = result as OkObjectResult;
        Assert.IsNotNull(ok);
        var json = System.Text.Json.JsonSerializer.Serialize(ok.Value);
        StringAssert.Contains(json, "plain-text");
    }

    [TestMethod]
    public void AsymmetricEncrypt_ArgumentException_Should_ReturnBadRequest()
    {
        var controller = CreateController(new FlexibleCryptographyService
        {
            AsymmetricEncryptFunc = _ => throw new ArgumentException("bad")
        });

        var result = controller.AsymmetricEncrypt(new CryptographyServiceAsymmetricEncryptRequest { PlainText = "x" });

        Assert.IsInstanceOfType<BadRequestObjectResult>(result);
    }

    [TestMethod]
    public void AsymmetricDecrypt_Success_Should_ReturnOk()
    {
        var controller = CreateController(new FlexibleCryptographyService
        {
            AsymmetricDecryptFunc = _ => "decrypted"
        });

        var result = controller.AsymmetricDecrypt(new CryptographyServiceAsymmetricDecryptRequest { CipherTextBase64 = "cipher" });

        var ok = result as OkObjectResult;
        Assert.IsNotNull(ok);
        var json = System.Text.Json.JsonSerializer.Serialize(ok.Value);
        StringAssert.Contains(json, "decrypted");
    }

    [TestMethod]
    public void Sign_ArgumentException_Should_ReturnBadRequest()
    {
        var controller = CreateController(new FlexibleCryptographyService
        {
            SignFunc = _ => throw new ArgumentException("bad")
        });

        var result = controller.Sign(new CryptographyServiceSignatureRequest { PlainText = "msg" });

        Assert.IsInstanceOfType<BadRequestObjectResult>(result);
    }

    [TestMethod]
    public void VerifySignature_Success_Should_ReturnOk()
    {
        var controller = CreateController(new FlexibleCryptographyService
        {
            VerifySignatureFunc = (_, _) => false
        });

        var result = controller.VerifySignature(new CryptographyServiceVerifySignatureRequest
        {
            PlainText = "msg",
            SignatureBase64 = "sig"
        });

        var ok = result as OkObjectResult;
        Assert.IsNotNull(ok);
        var json = System.Text.Json.JsonSerializer.Serialize(ok.Value);
        StringAssert.Contains(json, "false");
    }

    [TestMethod]
    public void Hash_ArgumentException_Should_ReturnBadRequest()
    {
        var controller = CreateController(new FlexibleCryptographyService
        {
            HashFunc = _ => throw new ArgumentException("bad")
        });

        var result = controller.Hash(new CryptographyServiceHashRequest { PlainText = "msg" });

        Assert.IsInstanceOfType<BadRequestObjectResult>(result);
    }

    private static CryptographyController CreateController(ICryptographyService service)
        => new(NullLogger<CryptographyController>.Instance, service);

    private sealed class FlexibleCryptographyService : ICryptographyService
    {
        public Func<int, (string KeyBase64, string IvBase64)>? GenerateSymmetricKeyFunc { get; set; }
        public Func<string, string>? SymmetricEncryptFunc { get; set; }
        public Func<string, string>? SymmetricDecryptFunc { get; set; }
        public Func<int, (string PublicKeyPem, string PrivateKeyPem)>? GenerateRsaKeyPairFunc { get; set; }
        public Func<string, string>? AsymmetricEncryptFunc { get; set; }
        public Func<string, string>? AsymmetricDecryptFunc { get; set; }
        public Func<string, string>? SignFunc { get; set; }
        public Func<string, string, bool>? VerifySignatureFunc { get; set; }
        public Func<string, string>? HashFunc { get; set; }
        public Func<string, string, bool>? VerifyHashFunc { get; set; }

        public (string KeyBase64, string IvBase64) GenerateSymmetricKey(int keySizeBits = 256)
            => GenerateSymmetricKeyFunc?.Invoke(keySizeBits) ?? ("k", "iv");

        public string SymmetricEncrypt(string plainText)
            => SymmetricEncryptFunc?.Invoke(plainText) ?? "cipher";

        public string SymmetricDecrypt(string cipherTextBase64)
            => SymmetricDecryptFunc?.Invoke(cipherTextBase64) ?? "plain";

        public (string PublicKeyPem, string PrivateKeyPem) GenerateRsaKeyPair(int keySizeBits = 2048)
            => GenerateRsaKeyPairFunc?.Invoke(keySizeBits) ?? ("pub", "pri");

        public string AsymmetricEncrypt(string plainText)
            => AsymmetricEncryptFunc?.Invoke(plainText) ?? "cipher";

        public string AsymmetricDecrypt(string cipherTextBase64)
            => AsymmetricDecryptFunc?.Invoke(cipherTextBase64) ?? "plain";

        public string Sign(string plainText)
            => SignFunc?.Invoke(plainText) ?? "sig";

        public bool VerifySignature(string plainText, string signatureBase64)
            => VerifySignatureFunc?.Invoke(plainText, signatureBase64) ?? true;

        public string Hash(string plainText)
            => HashFunc?.Invoke(plainText) ?? "hash";

        public bool VerifyHash(string plainText, string hashValue)
            => VerifyHashFunc?.Invoke(plainText, hashValue) ?? true;
    }
}
