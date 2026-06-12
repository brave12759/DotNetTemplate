using Microsoft.AspNetCore.Mvc;
using Template.BusinessRule.CryptographyService.Models;
using Template.BusinessRule.CryptographyService.Services;

namespace Template.WebApi.Controllers;

/// <summary>
/// 加解密工具控制器（供後端開發與內部整合使用）。
/// </summary>
/// <remarks>
/// 建立加解密控制器。
/// </remarks>
/// <param name="logger">控制器日誌。</param>
/// <param name="cryptographyService">加解密服務。</param>
public class CryptographyController(
    ILogger<CryptographyController> logger,
    ICryptographyService cryptographyService) : AuthenticationController<CryptographyController>(logger)
{
    private readonly ICryptographyService _cryptographyService = cryptographyService;

    /// <summary>
    /// 產生對稱金鑰與 IV。
    /// </summary>
    /// <param name="keySizeBits">AES 金鑰位元長度（128／192／256）。</param>
    /// <returns>Base64 格式金鑰與 IV。</returns>
    [HttpPost]
    public IActionResult GenerateSymmetricKey([FromQuery] int keySizeBits = 256)
    {
        try
        {
            var key = _cryptographyService.GenerateSymmetricKey(keySizeBits);
            return Ok(new
            {
                key.KeyBase64,
                key.IvBase64,
                keySizeBits
            });
        }
        catch (ArgumentException)
        {
            return BadRequest("輸入參數有誤，請確認金鑰長度（128／192／256）。");
        }
    }

    /// <summary>
    /// 以設定檔中的對稱金鑰進行 AES 加密。
    /// </summary>
    /// <param name="request">對稱加密請求。</param>
    /// <returns>Base64 密文。</returns>
    [HttpPost]
    public IActionResult SymmetricEncrypt([FromBody] CryptographyServiceSymmetricEncryptRequest request)
    {
        try
        {
            var cipherTextBase64 = _cryptographyService.SymmetricEncrypt(request.PlainText);
            return Ok(new { CipherTextBase64 = cipherTextBase64 });
        }
        catch (ArgumentException)
        {
            return BadRequest("輸入參數有誤，請確認明文內容。");
        }
    }

    /// <summary>
    /// 以設定檔中的對稱金鑰進行 AES 解密。
    /// </summary>
    /// <param name="request">對稱解密請求。</param>
    /// <returns>明文結果。</returns>
    [HttpPost]
    public IActionResult SymmetricDecrypt([FromBody] CryptographyServiceSymmetricDecryptRequest request)
    {
        try
        {
            var plainText = _cryptographyService.SymmetricDecrypt(request.CipherTextBase64);
            return Ok(new { PlainText = plainText });
        }
        catch (ArgumentException)
        {
            return BadRequest("輸入參數有誤，請確認密文格式（Base64）。");
        }
    }

    /// <summary>
    /// 產生 RSA 公私鑰。
    /// </summary>
    /// <param name="request">RSA 金鑰產生請求。</param>
    /// <returns>PEM 格式公私鑰。</returns>
    [HttpPost]
    public IActionResult GenerateRsaKeyPair([FromBody] CryptographyServiceGenerateRsaKeyPairRequest request)
    {
        try
        {
            var pair = _cryptographyService.GenerateRsaKeyPair(request.KeySizeBits);
            return Ok(new
            {
                pair.PublicKeyPem,
                pair.PrivateKeyPem,
                request.KeySizeBits
            });
        }
        catch (ArgumentException)
        {
            return BadRequest("輸入參數有誤，RSA 金鑰長度建議至少 2048 位元。");
        }
    }

    /// <summary>
    /// 以設定檔中的 RSA 公鑰進行加密。
    /// </summary>
    /// <param name="request">非對稱加密請求。</param>
    /// <returns>Base64 密文。</returns>
    [HttpPost]
    public IActionResult AsymmetricEncrypt([FromBody] CryptographyServiceAsymmetricEncryptRequest request)
    {
        try
        {
            var cipherTextBase64 = _cryptographyService.AsymmetricEncrypt(request.PlainText);
            return Ok(new { CipherTextBase64 = cipherTextBase64 });
        }
        catch (ArgumentException)
        {
            return BadRequest("輸入參數有誤，請確認明文內容。");
        }
    }

    /// <summary>
    /// 以設定檔中的 RSA 私鑰進行解密。
    /// </summary>
    /// <param name="request">非對稱解密請求。</param>
    /// <returns>明文結果。</returns>
    [HttpPost]
    public IActionResult AsymmetricDecrypt([FromBody] CryptographyServiceAsymmetricDecryptRequest request)
    {
        try
        {
            var plainText = _cryptographyService.AsymmetricDecrypt(request.CipherTextBase64);
            return Ok(new { PlainText = plainText });
        }
        catch (ArgumentException)
        {
            return BadRequest("輸入參數有誤，請確認密文格式（Base64）。");
        }
    }

    /// <summary>
    /// 以設定檔中的 RSA 私鑰進行簽章。
    /// </summary>
    /// <param name="request">簽章請求。</param>
    /// <returns>Base64 簽章字串。</returns>
    [HttpPost]
    public IActionResult Sign([FromBody] CryptographyServiceSignatureRequest request)
    {
        try
        {
            var signatureBase64 = _cryptographyService.Sign(request.PlainText);
            return Ok(new { SignatureBase64 = signatureBase64 });
        }
        catch (ArgumentException)
        {
            return BadRequest("輸入參數有誤，請確認明文內容。");
        }
    }

    /// <summary>
    /// 以設定檔中的 RSA 公鑰驗章。
    /// </summary>
    /// <param name="request">驗章請求。</param>
    /// <returns>驗章結果。</returns>
    [HttpPost]
    public IActionResult VerifySignature([FromBody] CryptographyServiceVerifySignatureRequest request)
    {
        try
        {
            var isValid = _cryptographyService.VerifySignature(
                request.PlainText,
                request.SignatureBase64);

            return Ok(new { IsValid = isValid });
        }
        catch (ArgumentException)
        {
            return BadRequest("輸入參數有誤，請確認明文與簽章格式（Base64）。");
        }
    }

    /// <summary>
    /// 產生 PBKDF2 雜湊。
    /// </summary>
    /// <param name="request">雜湊請求。</param>
    /// <returns>雜湊字串。</returns>
    [HttpPost]
    public IActionResult Hash([FromBody] CryptographyServiceHashRequest request)
    {
        try
        {
            var hashValue = _cryptographyService.Hash(request.PlainText);
            return Ok(new { HashValue = hashValue });
        }
        catch (ArgumentException)
        {
            return BadRequest("輸入參數有誤，請確認明文內容。");
        }
    }

    /// <summary>
    /// 驗證 PBKDF2 雜湊。
    /// </summary>
    /// <param name="request">雜湊驗證請求。</param>
    /// <returns>驗證結果。</returns>
    [HttpPost]
    public IActionResult VerifyHash([FromBody] CryptographyServiceVerifyHashRequest request)
    {
        try
        {
            var isValid = _cryptographyService.VerifyHash(request.PlainText, request.HashValue);
            return Ok(new { IsValid = isValid });
        }
        catch (ArgumentException)
        {
            return BadRequest("輸入參數有誤，請確認明文與雜湊格式。");
        }
    }
}
