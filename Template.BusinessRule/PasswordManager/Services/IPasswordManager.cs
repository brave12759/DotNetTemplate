namespace Template.BusinessRule.PasswordManager.Services;

/// <summary>
/// 密碼規則、雜湊與驗證服務介面。
/// </summary>
public interface IPasswordManager
{
    /// <summary>
    /// 驗證新密碼是否符合系統密碼規則。
    /// </summary>
    void ValidateNewPassword(string password);

    /// <summary>
    /// 將密碼轉成可儲存的雜湊字串。
    /// </summary>
    string HashForStorage(string password);

    /// <summary>
    /// 驗證密碼是否符合已儲存的雜湊字串。
    /// </summary>
    bool Verify(string password, string storedHash);
}
