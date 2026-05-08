namespace Template.Common.Settings;

/// <summary>
/// 加解密金鑰設定。
/// </summary>
public class CryptographyKeySettings
{
    /// <summary>
    /// 設定區段名稱。
    /// </summary>
    public const string SectionName = "CryptographyKeySettings";

    /// <summary>
    /// AES 對稱金鑰（Base64，16/24/32 bytes）
    /// </summary>
    public string SymmetricKeyBase64 { get; set; } = string.Empty;

    /// <summary>
    /// AES IV（Base64，16 bytes）
    /// </summary>
    public string SymmetricIvBase64 { get; set; } = string.Empty;

    /// <summary>
    /// RSA 公鑰（PEM）
    /// </summary>
    public string RsaPublicKeyPem { get; set; } = string.Empty;

    /// <summary>
    /// RSA 私鑰（PEM）
    /// </summary>
    public string RsaPrivateKeyPem { get; set; } = string.Empty;
}
