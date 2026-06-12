using System.ComponentModel.DataAnnotations;

namespace Template.WebApi.Models.Auth;

public class TokenValidateRequest
{
    [Required]
    public string Token { get; set; } = string.Empty;
}
