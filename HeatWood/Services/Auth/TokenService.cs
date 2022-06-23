using System.Security.Authentication;
using System.Security.Claims;
using HeatWood.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace HeatWood.Services.Auth;

public sealed class TokenService : ITokenService
{
    private readonly IAuthManager<IdentityUser> _authManager;
    private readonly IJwtBearerManager _jwtBearerManager;
    private readonly JwtBearerSettings _jwtBearerSettings;

    public TokenService(IAuthManager<IdentityUser> authManager, IJwtBearerManager jwtBearerManager,
        IOptions<JwtBearerSettings> jwtBearerSettings)
    {
        _authManager = authManager;
        _jwtBearerManager = jwtBearerManager;
        _jwtBearerSettings = jwtBearerSettings.Value;
    }

    public async Task<JwtBearerTokens> IssueBearerTokensAsync(Credentials credentials)
    {
        IdentityUser user = await _authManager.AuthenticateAsync(credentials) ?? throw new InvalidCredentialException();
        IReadOnlyCollection<Claim> claims = new[] {new Claim(ClaimTypes.NameIdentifier, user.Id)};
        
        BearerToken accessToken = _jwtBearerManager.CreateToken(claims, _jwtBearerSettings.AccessToken.LifeSpan);
        BearerToken refreshToken = _jwtBearerManager.CreateToken(claims, _jwtBearerSettings.RefreshToken.LifeSpan);

        await _authManager.StoreRefreshTokenAsync(user, refreshToken);

        return new JwtBearerTokens(accessToken.Value, refreshToken.Value);
    }

    public async Task<JwtBearerTokens> RefreshBearerTokensAsync(BearerToken refreshToken)
    {
        ClaimsPrincipal principal = _jwtBearerManager.GetPrincipalFromToken(refreshToken);
        IdentityUser user = await _authManager.AuthenticateAsync(principal) ??
                            throw new SecurityTokenException("Could find user for the given token.");

        BearerToken? storedRefreshToken = await _authManager.GetRefreshTokenAsync(user);
        if (storedRefreshToken is null || !storedRefreshToken.Equals(refreshToken))
        {
            throw new SecurityTokenException("Invalid refresh token.");
        }

        IReadOnlyCollection<Claim> claims = new[] {new Claim(ClaimTypes.NameIdentifier, user.Id)};
        
        BearerToken newAccessToken = _jwtBearerManager.CreateToken(claims, _jwtBearerSettings.AccessToken.LifeSpan);
        BearerToken newRefreshToken = _jwtBearerManager.CreateToken(claims, _jwtBearerSettings.RefreshToken.LifeSpan);

        await _authManager.StoreRefreshTokenAsync(user, newRefreshToken);

        return new JwtBearerTokens(newAccessToken.Value, newRefreshToken.Value);
    }

    public async Task RevokeRefreshTokenAsync(BearerToken refreshToken)
    {
        ClaimsPrincipal principal = _jwtBearerManager.GetPrincipalFromToken(refreshToken);

        IdentityUser? user = await _authManager.AuthenticateAsync(principal) ??
                             throw new SecurityTokenException("Could find user for the given token.");

        await _authManager.DeleteRefreshTokenAsync(user);
    }
}