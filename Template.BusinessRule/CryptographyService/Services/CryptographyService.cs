using System.Security.Cryptography;
using System.Text;
using Template.Common.Services;
using Template.Common.Settings;

namespace Template.BusinessRule.CryptographyService.Services;

/// <summary>
/// 加解密與雜湊服務實作。
/// </summary>
public class CryptographyService : ICryptographyService
{
    private readonly CryptographyKeySettings _keySettings;
    private readonly HashSettings _hashSettings;

    /// <summary>
    /// 建立加解密服務。
    /// </summary>
    /// <param name="keySettings">加解密金鑰設定。</param>
    /// <param name="hashSettings">雜湊設定。</param>
    public CryptographyService(CryptographyKeySettings keySettings, HashSettings hashSettings)
    {
        _keySettings = keySettings ?? throw new ArgumentNullException(nameof(keySettings));
        _hashSettings = hashSettings ?? throw new ArgumentNullException(nameof(hashSettings));
    }

    /// <inheritdoc />
    public (string KeyBase64, string IvBase64) GenerateSymmetricKey(int keySizeBits = 256)
    {
        if (keySizeBits is not (128 or 192 or 256))
            throw new ArgumentOutOfRangeException(nameof(keySizeBits), "AES 金鑰長度僅支援 128/192/256 位元。");

        var key = RandomNumberGenerator.GetBytes(keySizeBits / 8);
        var iv = RandomNumberGenerator.GetBytes(16);

        return (Convert.ToBase64String(key), Convert.ToBase64String(iv));
    }

    /// <inheritdoc />
    public string SymmetricEncrypt(string plainText)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plainText);

        var key = DecodeBase64(GetRequiredValue(_keySettings.SymmetricKeyBase64, nameof(_keySettings.SymmetricKeyBase64)), nameof(_keySettings.SymmetricKeyBase64));
        var iv = DecodeBase64(GetRequiredValue(_keySettings.SymmetricIvBase64, nameof(_keySettings.SymmetricIvBase64)), nameof(_keySettings.SymmetricIvBase64));

        ValidateAesKeyAndIv(key, iv);

        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        using var encryptor = aes.CreateEncryptor();
        var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        return Convert.ToBase64String(cipherBytes);
    }

    /// <inheritdoc />
    public string SymmetricDecrypt(string cipherTextBase64)
    {
        var cipherBytes = DecodeBase64(cipherTextBase64, nameof(cipherTextBase64));
        var key = DecodeBase64(GetRequiredValue(_keySettings.SymmetricKeyBase64, nameof(_keySettings.SymmetricKeyBase64)), nameof(_keySettings.SymmetricKeyBase64));
        var iv = DecodeBase64(GetRequiredValue(_keySettings.SymmetricIvBase64, nameof(_keySettings.SymmetricIvBase64)), nameof(_keySettings.SymmetricIvBase64));

        ValidateAesKeyAndIv(key, iv);

        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var decryptor = aes.CreateDecryptor();
        var plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);

        return Encoding.UTF8.GetString(plainBytes);
    }

    /// <inheritdoc />
    public (string PublicKeyPem, string PrivateKeyPem) GenerateRsaKeyPair(int keySizeBits = 2048)
    {
        if (keySizeBits < 2048)
            throw new ArgumentOutOfRangeException(nameof(keySizeBits), "RSA 金鑰長度建議至少 2048 位元。");

        using var rsa = RSA.Create(keySizeBits);
        var publicKeyPem = rsa.ExportRSAPublicKeyPem();
        var privateKeyPem = rsa.ExportRSAPrivateKeyPem();

        return (publicKeyPem, privateKeyPem);
    }

    /// <inheritdoc />
    public string AsymmetricEncrypt(string plainText)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plainText);
        var publicKeyPem = GetRequiredValue(_keySettings.RsaPublicKeyPem, nameof(_keySettings.RsaPublicKeyPem));

        using var rsa = RSA.Create();
        rsa.ImportFromPem(publicKeyPem);

        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var cipherBytes = rsa.Encrypt(plainBytes, RSAEncryptionPadding.OaepSHA256);

        return Convert.ToBase64String(cipherBytes);
    }

    /// <inheritdoc />
    public string AsymmetricDecrypt(string cipherTextBase64)
    {
        var cipherBytes = DecodeBase64(cipherTextBase64, nameof(cipherTextBase64));
        var privateKeyPem = GetRequiredValue(_keySettings.RsaPrivateKeyPem, nameof(_keySettings.RsaPrivateKeyPem));

        using var rsa = RSA.Create();
        rsa.ImportFromPem(privateKeyPem);

        var plainBytes = rsa.Decrypt(cipherBytes, RSAEncryptionPadding.OaepSHA256);
        return Encoding.UTF8.GetString(plainBytes);
    }

    /// <inheritdoc />
    public string Sign(string plainText)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plainText);
        var privateKeyPem = GetRequiredValue(_keySettings.RsaPrivateKeyPem, nameof(_keySettings.RsaPrivateKeyPem));

        using var rsa = RSA.Create();
        rsa.ImportFromPem(privateKeyPem);

        var data = Encoding.UTF8.GetBytes(plainText);
        var signature = rsa.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        return Convert.ToBase64String(signature);
    }

    /// <inheritdoc />
    public bool VerifySignature(string plainText, string signatureBase64)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plainText);
        var publicKeyPem = GetRequiredValue(_keySettings.RsaPublicKeyPem, nameof(_keySettings.RsaPublicKeyPem));

        var signature = DecodeBase64(signatureBase64, nameof(signatureBase64));

        using var rsa = RSA.Create();
        rsa.ImportFromPem(publicKeyPem);

        var data = Encoding.UTF8.GetBytes(plainText);
        return rsa.VerifyData(data, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
    }

    /// <inheritdoc />
    public string Hash(string plainText)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plainText);

        var iterations = _hashSettings.Iterations;

        if (iterations < 10000)
            throw new ArgumentOutOfRangeException(nameof(iterations), "雜湊迭代次數建議至少 10000。");

        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(plainText),
            salt,
            iterations,
            HashAlgorithmName.SHA256,
            32);

        return $"PBKDF2$SHA256${iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    /// <inheritdoc />
    public bool VerifyHash(string plainText, string hashValue)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plainText);
        ArgumentException.ThrowIfNullOrWhiteSpace(hashValue);

        var parts = hashValue.Split('$');
        if (parts.Length != 5 || !string.Equals(parts[0], "PBKDF2", StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(parts[1], "SHA256", StringComparison.OrdinalIgnoreCase))
        {
            throw new FormatException("雜湊格式不正確，預期為 PBKDF2$SHA256$iterations$salt$hash。");
        }

        if (!int.TryParse(parts[2], out var iterations) || iterations < 1)
            throw new FormatException("雜湊中的 iterations 格式不正確。");

        var salt = DecodeBase64(parts[3], "salt");
        var expectedHash = DecodeBase64(parts[4], "hash");

        var actualHash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(plainText),
            salt,
            iterations,
            HashAlgorithmName.SHA256,
            expectedHash.Length);

        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }

    /// <summary>
    /// 將 Base64 字串轉為位元組陣列。
    /// </summary>
    /// <param name="value">Base64 字串。</param>
    /// <param name="parameterName">來源參數名稱。</param>
    /// <returns>解碼後位元組陣列。</returns>
    /// <summary>
    /// 解碼 Base64 字串；格式錯誤時用參數名稱回報例外。
    /// </summary>
    private static byte[] DecodeBase64(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        try
        {
            return Convert.FromBase64String(value);
        }
        catch (FormatException ex)
        {
            throw new FormatException($"{parameterName} 不是有效的 Base64。", ex);
        }
    }

    /// <summary>
    /// 取得必要設定值，若空白則拋出例外。
    /// </summary>
    /// <param name="value">設定值。</param>
    /// <param name="fieldName">設定欄位名稱。</param>
    /// <returns>非空設定值。</returns>
    /// <summary>
    /// 取得必要字串設定值；空白時丟出欄位名稱對應的例外。
    /// </summary>
    private static string GetRequiredValue(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"CryptographyKeySettings.{fieldName} 未設定，請於 appsettings 設定。");

        return value;
    }

    /// <summary>
    /// 驗證 AES 金鑰與 IV 長度。
    /// </summary>
    /// <param name="key">AES 金鑰位元組。</param>
    /// <param name="iv">AES IV 位元組。</param>
    /// <summary>
    /// 驗證 AES 金鑰與 IV 長度是否符合支援規格。
    /// </summary>
    private static void ValidateAesKeyAndIv(byte[] key, byte[] iv)
    {
        if (key.Length is not (16 or 24 or 32))
            throw new ArgumentException("AES 金鑰長度需為 16/24/32 bytes。", nameof(key));

        if (iv.Length != 16)
            throw new ArgumentException("AES IV 長度需為 16 bytes。", nameof(iv));
    }
}
