using System.ComponentModel.DataAnnotations;

namespace Template.WebApi.Models.Auth;

public class SsoLoginRequest
{
    [Required]
    public string ClientId { get; set; } = string.Empty;

    [Required]
    public string ClientSecret { get; set; } = string.Empty;
}
