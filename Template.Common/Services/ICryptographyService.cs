namespace Template.Common.Services;

/// <summary>
/// 加解密與雜湊服務介面。
/// </summary>
public interface ICryptographyService
{
    /// <summary>
    /// 產生 AES 對稱金鑰與 IV。
    /// </summary>
    /// <param name="keySizeBits">金鑰位元長度，僅支援 128/192/256。</param>
    /// <returns>Base64 格式的金鑰與 IV。</returns>
    (string KeyBase64, string IvBase64) GenerateSymmetricKey(int keySizeBits = 256);

    /// <summary>
    /// 使用設定檔中的對稱金鑰進行 AES 加密。
    /// </summary>
    /// <param name="plainText">明文字串。</param>
    /// <returns>Base64 密文。</returns>
    string SymmetricEncrypt(string plainText);

    /// <summary>
    /// 使用設定檔中的對稱金鑰進行 AES 解密。
    /// </summary>
    /// <param name="cipherTextBase64">Base64 密文。</param>
    /// <returns>解密後明文。</returns>
    string SymmetricDecrypt(string cipherTextBase64);

    /// <summary>
    /// 產生 RSA 公私鑰。
    /// </summary>
    /// <param name="keySizeBits">金鑰位元長度，建議至少 2048。</param>
    /// <returns>PEM 格式公鑰與私鑰。</returns>
    (string PublicKeyPem, string PrivateKeyPem) GenerateRsaKeyPair(int keySizeBits = 2048);

    /// <summary>
    /// 使用設定檔中的 RSA 公鑰加密。
    /// </summary>
    /// <param name="plainText">明文字串。</param>
    /// <returns>Base64 密文。</returns>
    string AsymmetricEncrypt(string plainText);

    /// <summary>
    /// 使用設定檔中的 RSA 私鑰解密。
    /// </summary>
    /// <param name="cipherTextBase64">Base64 密文。</param>
    /// <returns>解密後明文。</returns>
    string AsymmetricDecrypt(string cipherTextBase64);

    /// <summary>
    /// 使用設定檔中的 RSA 私鑰簽章。
    /// </summary>
    /// <param name="plainText">要簽章的原文。</param>
    /// <returns>Base64 簽章字串。</returns>
    string Sign(string plainText);

    /// <summary>
    /// 使用設定檔中的 RSA 公鑰驗章。
    /// </summary>
    /// <param name="plainText">原文。</param>
    /// <param name="signatureBase64">Base64 簽章字串。</param>
    /// <returns>驗章是否成功。</returns>
    bool VerifySignature(string plainText, string signatureBase64);

    /// <summary>
    /// 使用 PBKDF2 產生不可逆雜湊字串。
    /// </summary>
    /// <param name="plainText">原始字串。</param>
    /// <returns>可供驗證的雜湊結果。</returns>
    string Hash(string plainText);

    /// <summary>
    /// 驗證 PBKDF2 雜湊。
    /// </summary>
    /// <param name="plainText">原始字串。</param>
    /// <param name="hashValue">既有雜湊字串。</param>
    /// <returns>驗證是否成功。</returns>
    bool VerifyHash(string plainText, string hashValue);
}
