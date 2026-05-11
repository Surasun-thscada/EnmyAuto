using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace EnmyAuto.Api.Configuration;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required] public string SecretKey { get; init; } = string.Empty;
    [Required] public string Issuer    { get; init; } = string.Empty;
    [Required] public string Audience  { get; init; } = string.Empty;

    /// <summary>Access token lifetime in minutes.</summary>
    public int AccessTokenExpiryMinutes  { get; init; } = 60;

    /// <summary>Refresh token lifetime in days.</summary>
    public int RefreshTokenExpiryDays    { get; init; } = 30;

    public SymmetricSecurityKey GetSigningKey() =>
        new(Encoding.UTF8.GetBytes(SecretKey));
}
