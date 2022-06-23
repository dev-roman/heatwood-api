using System.Security.Authentication;
using HeatWood.Models;
using HeatWood.Services.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace HeatWood.Areas.Admin.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class TokenController : ControllerBase
{
    private readonly ITokenService _tokenService;

    public TokenController(ITokenService tokenService)
    {
        _tokenService = tokenService;
    }

    [HttpPost("generate")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(JwtBearerTokens))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Generate([FromBody] Credentials credentials)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                throw new ArgumentException();
            }

            JwtBearerTokens tokens = await _tokenService.IssueBearerTokensAsync(credentials);

            return Ok(tokens);
        }
        catch (InvalidCredentialException)
        {
            return Unauthorized();
        }
        catch (ArgumentException)
        {
            return BadRequest();
        }
    }

    [HttpPost("refresh")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(JwtBearerTokens))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Refresh([FromHeader] BearerToken refreshToken)
    {
        try
        {
            JwtBearerTokens tokens = await _tokenService.RefreshBearerTokensAsync(refreshToken);

            return Ok(tokens);
        }
        catch (SecurityTokenException)
        {
            return Unauthorized();
        }
        catch (ArgumentException)
        {
            return BadRequest();
        }
    }

    [HttpPost("revoke")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Revoke([FromHeader] BearerToken refreshToken)
    {
        try
        {
            await _tokenService.RevokeRefreshTokenAsync(refreshToken);

            return NoContent();
        }
        catch (SecurityTokenException)
        {
            return Unauthorized();
        }
        catch (ArgumentException)
        {
            return BadRequest();
        }
    }
}