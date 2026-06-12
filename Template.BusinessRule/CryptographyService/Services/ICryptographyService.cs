namespace Template.BusinessRule.CryptographyService.Services;

/// <summary>
/// 加解密、簽章與雜湊服務介面。
/// </summary>
public interface ICryptographyService
{
    /// <summary>
    /// 產生 AES 金鑰與 IV。
    /// </summary>
    (string KeyBase64, string IvBase64) GenerateSymmetricKey(int keySizeBits = 256);

    /// <summary>
    /// 使用設定的 AES 金鑰與 IV 加密明文。
    /// </summary>
    string SymmetricEncrypt(string plainText);

    /// <summary>
    /// 使用設定的 AES 金鑰與 IV 解密密文。
    /// </summary>
    string SymmetricDecrypt(string cipherTextBase64);

    /// <summary>
    /// 產生 RSA 公私鑰組。
    /// </summary>
    (string PublicKeyPem, string PrivateKeyPem) GenerateRsaKeyPair(int keySizeBits = 2048);

    /// <summary>
    /// 使用設定的 RSA 公鑰加密明文。
    /// </summary>
    string AsymmetricEncrypt(string plainText);

    /// <summary>
    /// 使用設定的 RSA 私鑰解密密文。
    /// </summary>
    string AsymmetricDecrypt(string cipherTextBase64);

    /// <summary>
    /// 使用設定的 RSA 私鑰簽章明文。
    /// </summary>
    string Sign(string plainText);

    /// <summary>
    /// 使用設定的 RSA 公鑰驗證簽章。
    /// </summary>
    bool VerifySignature(string plainText, string signatureBase64);

    /// <summary>
    /// 使用 PBKDF2 產生不可逆雜湊。
    /// </summary>
    string Hash(string plainText);

    /// <summary>
    /// 驗證明文是否符合 PBKDF2 雜湊值。
    /// </summary>
    bool VerifyHash(string plainText, string hashValue);
}
