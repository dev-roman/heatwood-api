using System.Security.Claims;
using HeatWood.Models;

namespace HeatWood.Services.Auth;

public interface IJwtBearerManager
{
    public BearerToken CreateToken(IEnumerable<Claim> claims, TimeSpan tokenLifeSpan);
    
    public ClaimsPrincipal GetPrincipalFromToken(BearerToken token);
}