using Microsoft.Extensions.DependencyInjection;
using Template.Common.Services;

namespace Template.BusinessRule.PasswordManager.Services;

/// <summary>
/// 密碼管理服務，封裝密碼規則與雜湊驗證邏輯。
/// </summary>
public class PasswordManager(IServiceProvider serviceProvider) : IPasswordManager
{
    private readonly Lazy<ICryptographyService> _cryptographyService = new(() =>
        serviceProvider.GetRequiredService<ICryptographyService>());

    /// <inheritdoc />
    public void ValidateNewPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("密碼不可為空。", nameof(password));

        if (password.Length < 8)
            throw new ArgumentException("密碼長度至少需 8 碼。", nameof(password));

        var hasLetter = password.Any(char.IsLetter);
        var hasDigit = password.Any(char.IsDigit);

        if (!hasLetter || !hasDigit)
            throw new ArgumentException("密碼需同時包含英文字母與數字。", nameof(password));
    }

    /// <inheritdoc />
    public string HashForStorage(string password)
    {
        ValidateNewPassword(password);
        return _cryptographyService.Value.Hash(password);
    }

    /// <inheritdoc />
    public bool Verify(string password, string storedHash)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(storedHash))
            return false;

        return _cryptographyService.Value.VerifyHash(password, storedHash);
    }
}
