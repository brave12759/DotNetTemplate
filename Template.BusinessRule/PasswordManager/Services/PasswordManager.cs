using Microsoft.Extensions.DependencyInjection;
using Template.BusinessRule.CryptographyService.Services;

namespace Template.BusinessRule.PasswordManager.Services;

/// <summary>
/// 密碼規則、雜湊與驗證服務。
/// </summary>
public class PasswordManager(IServiceProvider serviceProvider) : IPasswordManager
{
    private readonly Lazy<ICryptographyService> _cryptographyService = new(() =>
        serviceProvider.GetRequiredService<ICryptographyService>());

    /// <inheritdoc />
    public void ValidateNewPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password is required.", nameof(password));

        if (password.Length < 12)
            throw new ArgumentException("Password must be at least 12 characters.", nameof(password));

        var hasUpper = password.Any(char.IsUpper);
        var hasLower = password.Any(char.IsLower);
        var hasDigit = password.Any(char.IsDigit);
        var hasSymbol = password.Any(ch => !char.IsLetterOrDigit(ch));

        if (!hasUpper || !hasLower || !hasDigit || !hasSymbol)
            throw new ArgumentException("Password must include uppercase letters, lowercase letters, digits, and symbols.", nameof(password));
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
