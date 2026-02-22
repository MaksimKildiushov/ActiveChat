using System.ComponentModel.DataAnnotations;

namespace Ac.Api.Models.Auth;

public record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password);
