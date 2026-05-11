using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using EnmyAuto.Api.Configuration;
using EnmyAuto.Api.Data;
using EnmyAuto.Api.Models;
using EnmyAuto.Api.Models.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace EnmyAuto.Api.Services.Auth;

public sealed class AuthService(
    ApplicationDbContext db,
    IOptions<JwtOptions> jwtOptions,
    ILogger<AuthService> logger) : IAuthService
{
    private readonly JwtOptions _jwt = jwtOptions.Value;

    // ── Register ──────────────────────────────────────────────────────────────

    public async Task<AuthResponse> RegisterAsync(
        RegisterRequest request, CancellationToken ct = default)
    {
        var exists = await db.Users
            .AnyAsync(u => u.Email == request.Email.ToLower(), ct);

        if (exists)
            throw new InvalidOperationException("An account with this email already exists.");

        var user = new User
        {
            Name         = request.Name.Trim(),
            Email        = request.Email.ToLower().Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
        };

        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("New user registered. UserId={Id}", user.Id);

        return await IssueTokensAsync(user, ct);
    }

    // ── Login ─────────────────────────────────────────────────────────────────

    public async Task<AuthResponse> LoginAsync(
        LoginRequest request, CancellationToken ct = default)
    {
        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email.ToLower(), ct);

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid email or password.");

        logger.LogInformation("User logged in. UserId={Id}", user.Id);

        return await IssueTokensAsync(user, ct);
    }

    // ── Refresh ───────────────────────────────────────────────────────────────

    public async Task<AuthResponse> RefreshAsync(
        string refreshToken, CancellationToken ct = default)
    {
        var tokenHash = HashToken(refreshToken);

        var user = await db.Users.FirstOrDefaultAsync(
            u => u.RefreshToken == tokenHash, ct);

        if (user is null || user.RefreshTokenExpiry < DateTime.UtcNow)
            throw new UnauthorizedAccessException("Refresh token is invalid or expired.");

        return await IssueTokensAsync(user, ct);
    }

    // ── Revoke (Logout) ───────────────────────────────────────────────────────

    public async Task RevokeAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await db.Users.FindAsync([userId], ct);
        if (user is null) return;

        user.RefreshToken       = null;
        user.RefreshTokenExpiry = null;
        await db.SaveChangesAsync(ct);
    }

    public async Task<UserDto> UpdateProfileAsync(
        Guid userId, UpdateProfileRequest request, CancellationToken ct = default)
    {
        var user = await db.Users.FindAsync([userId], ct)
            ?? throw new KeyNotFoundException("User not found.");

        user.Name = request.Name.Trim();
        await db.SaveChangesAsync(ct);

        return new UserDto(user.Id, user.Name, user.Email, user.QuotaLimit);
    }

    public async Task ChangePasswordAsync(
        Guid userId, ChangePasswordRequest request, CancellationToken ct = default)
    {
        var user = await db.Users.FindAsync([userId], ct)
            ?? throw new KeyNotFoundException("User not found.");

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            throw new UnauthorizedAccessException("Current password is incorrect.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAccountAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await db.Users.FindAsync([userId], ct);
        if (user is null) return;

        db.Users.Remove(user);
        await db.SaveChangesAsync(ct);
    }

    // ── Token generation ──────────────────────────────────────────────────────

    private async Task<AuthResponse> IssueTokensAsync(User user, CancellationToken ct)
    {
        var accessExpiry   = DateTime.UtcNow.AddMinutes(_jwt.AccessTokenExpiryMinutes);
        var accessToken    = BuildAccessToken(user, accessExpiry);

        var rawRefresh     = GenerateRefreshToken();
        user.RefreshToken       = HashToken(rawRefresh);
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(_jwt.RefreshTokenExpiryDays);
        await db.SaveChangesAsync(ct);

        return new AuthResponse(
            AccessToken:       accessToken,
            RefreshToken:      rawRefresh,
            AccessTokenExpiry: accessExpiry,
            User: new UserDto(user.Id, user.Name, user.Email, user.QuotaLimit));
    }

    private string BuildAccessToken(User user, DateTime expiry)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub,   user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Name,  user.Name),
            new Claim(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString()),
            new Claim("quota", user.QuotaLimit.ToString()),
        };

        var credentials = new SigningCredentials(
            _jwt.GetSigningKey(), SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer:             _jwt.Issuer,
            audience:           _jwt.Audience,
            claims:             claims,
            notBefore:          DateTime.UtcNow,
            expires:            expiry,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }

    // Store only the hash of the refresh token — raw token lives only in the response.
    private static string HashToken(string token)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(token);
        var hash  = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }
}
