namespace Template.Common.Services;

/// <summary>
/// 密碼管理服務介面，統一處理密碼規則、雜湊與驗證。
/// </summary>
public interface IPasswordManager
{
    /// <summary>
    /// 驗證新密碼是否符合規則。
    /// </summary>
    void ValidateNewPassword(string password);

    /// <summary>
    /// 將新密碼轉為可儲存的雜湊值。
    /// </summary>
    string HashForStorage(string password);

    /// <summary>
    /// 驗證明文密碼是否符合既有雜湊。
    /// </summary>
    bool Verify(string password, string storedHash);
}
