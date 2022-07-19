using System.Security.Claims;
using HeatWood.Models;
using HeatWood.Models.Auth;

namespace HeatWood.Services.Auth;

public interface IAuthManager<TUser> where TUser : class
{
    Task<TUser?> AuthenticateAsync(Credentials credentials);

    Task<TUser?> AuthenticateAsync(ClaimsPrincipal principal);
    
    Task<BearerToken?> GetRefreshTokenAsync(TUser user);

    Task StoreRefreshTokenAsync(TUser user, BearerToken token);
    
    Task DeleteRefreshTokenAsync(TUser user);
}