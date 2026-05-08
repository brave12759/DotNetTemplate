namespace Template.BusinessRule.CryptographyService.Models;

/// <summary>
/// RSA 金鑰產生請求。
/// </summary>
public class CryptographyServiceGenerateRsaKeyPairRequest
{
    /// <summary>
    /// RSA 金鑰長度（位元）。
    /// </summary>
    public int KeySizeBits { get; set; } = 2048;
}

/// <summary>
/// 對稱加密請求。
/// </summary>
public class CryptographyServiceSymmetricEncryptRequest
{
    /// <summary>
    /// 要加密的明文。
    /// </summary>
    public string PlainText { get; set; } = string.Empty;
}

/// <summary>
/// 對稱解密請求。
/// </summary>
public class CryptographyServiceSymmetricDecryptRequest
{
    /// <summary>
    /// 要解密的 Base64 密文。
    /// </summary>
    public string CipherTextBase64 { get; set; } = string.Empty;
}

/// <summary>
/// 非對稱加密請求。
/// </summary>
public class CryptographyServiceAsymmetricEncryptRequest
{
    /// <summary>
    /// 要加密的明文。
    /// </summary>
    public string PlainText { get; set; } = string.Empty;
}

/// <summary>
/// 非對稱解密請求。
/// </summary>
public class CryptographyServiceAsymmetricDecryptRequest
{
    /// <summary>
    /// 要解密的 Base64 密文。
    /// </summary>
    public string CipherTextBase64 { get; set; } = string.Empty;
}

/// <summary>
/// 簽章請求。
/// </summary>
public class CryptographyServiceSignatureRequest
{
    /// <summary>
    /// 要簽章的原文。
    /// </summary>
    public string PlainText { get; set; } = string.Empty;
}

/// <summary>
/// 驗章請求。
/// </summary>
public class CryptographyServiceVerifySignatureRequest
{
    /// <summary>
    /// 原文。
    /// </summary>
    public string PlainText { get; set; } = string.Empty;

    /// <summary>
    /// Base64 簽章內容。
    /// </summary>
    public string SignatureBase64 { get; set; } = string.Empty;
}

/// <summary>
/// 雜湊請求。
/// </summary>
public class CryptographyServiceHashRequest
{
    /// <summary>
    /// 要產生雜湊的原文。
    /// </summary>
    public string PlainText { get; set; } = string.Empty;
}

/// <summary>
/// 雜湊驗證請求。
/// </summary>
public class CryptographyServiceVerifyHashRequest
{
    /// <summary>
    /// 原文。
    /// </summary>
    public string PlainText { get; set; } = string.Empty;

    /// <summary>
    /// 既有雜湊內容。
    /// </summary>
    public string HashValue { get; set; } = string.Empty;
}
