using System.ComponentModel.DataAnnotations;

namespace Ac.Api.Models.Auth;

public record RegisterRequest(
    [Required, EmailAddress] string Email,
    [Required, MinLength(6)] string Password,
    string? DisplayName);
