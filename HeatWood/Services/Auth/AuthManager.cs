using System.Security.Claims;
using HeatWood.Models;
using HeatWood.Models.Auth;
using Microsoft.AspNetCore.Identity;

namespace HeatWood.Services.Auth;

public sealed class AuthManager<TUser> : IAuthManager<TUser> where TUser : class
{
    private const string LoginProvider = "JwtBearerProvider";
    private const string RefreshTokenName = "RefreshToken";

    private readonly UserManager<TUser> _userManager;

    public AuthManager(UserManager<TUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<TUser?> AuthenticateAsync(Credentials credentials)
    {
        TUser? user = await _userManager.FindByNameAsync(credentials.UserName);
        var authenticated = await _userManager.CheckPasswordAsync(user, credentials.Password);

        return authenticated ? user : null;
    }

    public async Task<TUser?> AuthenticateAsync(ClaimsPrincipal principal)
    {
        return await _userManager.GetUserAsync(principal);
    }

    public async Task<BearerToken?> GetRefreshTokenAsync(TUser user)
    {
        var token = await _userManager.GetAuthenticationTokenAsync(user, LoginProvider, RefreshTokenName);

        return token is not null ? new BearerToken {Value = token} : null;
    }

    public async Task StoreRefreshTokenAsync(TUser user, BearerToken token)
    {
        await _userManager.SetAuthenticationTokenAsync(user, LoginProvider, RefreshTokenName, token.Value);
    }

    public async Task DeleteRefreshTokenAsync(TUser user)
    {
        await _userManager.RemoveAuthenticationTokenAsync(user, LoginProvider, RefreshTokenName);
    }
}