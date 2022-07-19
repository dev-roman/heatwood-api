using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using HeatWood.Models;
using HeatWood.Models.Auth;
using Microsoft.IdentityModel.Tokens;

namespace HeatWood.Services.Auth;

public sealed class JwtBearerManager : IJwtBearerManager
{
    private readonly string _jwtSecret;
    private readonly JwtSecurityTokenHandler _tokenHandler;

    public JwtBearerManager(string jwtSecret)
    {
        _jwtSecret = jwtSecret;
        _tokenHandler = new JwtSecurityTokenHandler();
    }
    
    public BearerToken CreateToken(IEnumerable<Claim> claims, TimeSpan tokenLifeSpan)
    {
        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.Now + tokenLifeSpan,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_jwtSecret)),
                SecurityAlgorithms.HmacSha256Signature)
        };

        SecurityToken securityToken = _tokenHandler.CreateToken(descriptor);
        return new BearerToken {Value = _tokenHandler.WriteToken(securityToken)};
    }

    public ClaimsPrincipal GetPrincipalFromToken(BearerToken token)
    {
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey =
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecret)),
        };

        return _tokenHandler.ValidateToken(token.Value, validationParameters, out SecurityToken _);
    }
}