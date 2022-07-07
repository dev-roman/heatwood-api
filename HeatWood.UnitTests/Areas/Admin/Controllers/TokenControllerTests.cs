using System.Security.Authentication;
using FluentAssertions;
using HeatWood.Areas.Admin.Controllers;
using HeatWood.Models;
using HeatWood.Services.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Moq;

namespace HeatWood.UnitTests.Areas.Admin.Controllers;

public sealed class TokenControllerTests
{
    private readonly Mock<ITokenService> _mockAuthService;
    private readonly TokenController _sut;

    public TokenControllerTests()
    {
        _mockAuthService = new Mock<ITokenService>();

        _sut = new TokenController(_mockAuthService.Object);
    }

    [Fact]
    public async Task Generate_Success()
    {
        var bearerTokens = new JwtBearerTokens("a", "b");

        _mockAuthService
            .Setup(x => x.IssueBearerTokensAsync(It.IsAny<Credentials>()))
            .ReturnsAsync(bearerTokens);

        var result = await _sut.Generate(It.IsAny<Credentials>()) as ObjectResult;
        result!.StatusCode.Should().Be(200);

        var tokens = result.Value as JwtBearerTokens;
        tokens!.AccessToken.Should().Be("a");
        tokens.RefreshToken.Should().Be("b");
    }
    
    [Fact]
    public async Task Generate_Failure_InvalidCredentialException()
    {
        _mockAuthService
            .Setup(x => x.IssueBearerTokensAsync(It.IsAny<Credentials>()))
            .ThrowsAsync(new InvalidCredentialException());
        
        var result = await _sut.Generate(It.IsAny<Credentials>()) as StatusCodeResult;

        result!.StatusCode.Should().Be(401);
    }
    
    [Fact]
    public async Task Refresh_Success()
    {
        _mockAuthService
            .Setup(x => x.RefreshBearerTokensAsync(It.IsAny<BearerToken>()))
            .ReturnsAsync(new JwtBearerTokens("a", "b"));

        var result = await _sut.Refresh(It.IsAny<BearerToken>()) as ObjectResult;
        result!.StatusCode.Should().Be(200);
        
        var tokens = result.Value as JwtBearerTokens;
        tokens!.AccessToken.Should().Be("a");
        tokens.RefreshToken.Should().Be("b");
    }
    
    [Fact]
    public async Task Refresh_Failure_Unauthorized_SecurityTokenException()
    {
        _mockAuthService
            .Setup(x => x.RefreshBearerTokensAsync(It.IsAny<BearerToken>()))
            .ThrowsAsync(new SecurityTokenException());

        var result = await _sut.Refresh(It.IsAny<BearerToken>()) as StatusCodeResult;
        result!.StatusCode.Should().Be(401);
    }
    
    [Fact]
    public async Task Refresh_Failure_Unauthorized_SecurityTokenExpiredException()
    {
        _mockAuthService
            .Setup(x => x.RefreshBearerTokensAsync(It.IsAny<BearerToken>()))
            .ThrowsAsync(new SecurityTokenExpiredException());

        var result = await _sut.Refresh(It.IsAny<BearerToken>()) as StatusCodeResult;
        result!.StatusCode.Should().Be(401);
    }
    
    [Fact]
    public async Task Revoke_Success()
    {
        var result = await _sut.Revoke(It.IsAny<BearerToken>()) as StatusCodeResult;
        result!.StatusCode.Should().Be(204);
        
        _mockAuthService.Verify(x => x.RevokeRefreshTokenAsync(It.IsAny<BearerToken>()));
    }
    
    [Fact]
    public async Task Revoke_Failure_Unauthorized_SecurityTokenException()
    {
        _mockAuthService
            .Setup(x => x.RevokeRefreshTokenAsync(It.IsAny<BearerToken>()))
            .ThrowsAsync(new SecurityTokenException());
        
        var result = await _sut.Revoke(It.IsAny<BearerToken>()) as StatusCodeResult;
        
        result!.StatusCode.Should().Be(401);
    }
}