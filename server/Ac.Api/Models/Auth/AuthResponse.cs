namespace Ac.Api.Models.Auth;

public record AuthResponse(
    string Token,
    string Email,
    string? DisplayName,
    DateTime ExpiresAt);
