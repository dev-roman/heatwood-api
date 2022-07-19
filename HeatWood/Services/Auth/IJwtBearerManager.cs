using System.Security.Claims;
using HeatWood.Models;
using HeatWood.Models.Auth;

namespace HeatWood.Services.Auth;

public interface IJwtBearerManager
{
    public BearerToken CreateToken(IEnumerable<Claim> claims, TimeSpan tokenLifeSpan);
    
    public ClaimsPrincipal GetPrincipalFromToken(BearerToken token);
}