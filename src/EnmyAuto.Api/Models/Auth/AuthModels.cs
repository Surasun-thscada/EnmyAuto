using System.ComponentModel.DataAnnotations;

namespace EnmyAuto.Api.Models.Auth;

public sealed record RegisterRequest(
    [Required][MaxLength(150)]          string Name,
    [Required][EmailAddress]            string Email,
    [Required][MinLength(8)]            string Password
);

public sealed record LoginRequest(
    [Required][EmailAddress]            string Email,
    [Required]                          string Password
);

public sealed record RefreshTokenRequest(
    [Required]                          string RefreshToken
);

public sealed record AuthResponse(
    string      AccessToken,
    string      RefreshToken,
    DateTime    AccessTokenExpiry,
    UserDto     User
);

public sealed record UserDto(
    Guid    Id,
    string  Name,
    string  Email,
    int     QuotaLimit
);

public sealed record UpdateProfileRequest(
    [Required][MaxLength(150)] string Name
);

public sealed record ChangePasswordRequest(
    [Required]              string CurrentPassword,
    [Required][MinLength(8)] string NewPassword
);
