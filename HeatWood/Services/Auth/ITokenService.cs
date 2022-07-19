using HeatWood.Models;
using HeatWood.Models.Auth;

namespace HeatWood.Services.Auth;

public interface ITokenService
{
    Task<JwtBearerTokens> IssueBearerTokensAsync(Credentials credentials);

    Task<JwtBearerTokens> RefreshBearerTokensAsync(BearerToken refreshToken);

    Task RevokeRefreshTokenAsync(BearerToken refreshToken);
}