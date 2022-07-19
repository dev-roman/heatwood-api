using System.Security.Authentication;
using System.Security.Claims;
using FluentAssertions;
using HeatWood.Models;
using HeatWood.Models.Auth;
using HeatWood.Services.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Moq;

namespace HeatWood.UnitTests.Services.Auth;

public sealed class TokenServiceTests
{
    private readonly Mock<IAuthManager<IdentityUser>> _mockAuthManager;
    private readonly Mock<IJwtBearerManager> _mockJwtBearerManager;
    private readonly TokenService _sut;
    private readonly IdentityUser _user;
    private readonly OptionsWrapper<JwtBearerSettings> _mockJwtBearerSettings;

    public TokenServiceTests()
    {
        _mockAuthManager = new Mock<IAuthManager<IdentityUser>>();
        _mockJwtBearerManager = new Mock<IJwtBearerManager>();
        _mockJwtBearerSettings = new OptionsWrapper<JwtBearerSettings>(new JwtBearerSettings
        {
            AccessToken = new() {LifeSpan = TimeSpan.FromMinutes(1)},
            RefreshToken = new() {LifeSpan = TimeSpan.FromMinutes(2)}
        });

        _sut = new TokenService(_mockAuthManager.Object, _mockJwtBearerManager.Object, _mockJwtBearerSettings);
        _user = new IdentityUser {Id = "8E34AD86-3EEA-4030-8525-9DD22A473A40"};
    }

    [Fact]
    public async Task IssueBearerTokenAsync_ReturnsTokens()
    {
        // Arrange
        _mockAuthManager
            .Setup(x => x.AuthenticateAsync(It.IsAny<Credentials>()))
            .ReturnsAsync(_user);

        _mockJwtBearerManager
            .Setup(x => x.CreateToken(It.IsAny<IEnumerable<Claim>>(), _mockJwtBearerSettings.Value.AccessToken.LifeSpan))
            .Returns(new BearerToken {Value = "a"});

        _mockJwtBearerManager
            .Setup(x => x.CreateToken(It.IsAny<IEnumerable<Claim>>(), _mockJwtBearerSettings.Value.RefreshToken.LifeSpan))
            .Returns(new BearerToken {Value = "b"});

        // Act
        JwtBearerTokens result = await _sut.IssueBearerTokensAsync(It.IsAny<Credentials>());
        
        // Assert
        result.AccessToken.Should().BeEquivalentTo("a");
        result.RefreshToken.Should().BeEquivalentTo("b");

        _mockAuthManager.Verify(x => x.StoreRefreshTokenAsync(_user, It.IsAny<BearerToken>()));
    }

    [Fact]
    public async Task IssueBearerTokenAsync_InvalidCredentials_ThrowsInvalidCredentialException()
    {
        // Arrange
        _mockAuthManager
            .Setup(x => x.AuthenticateAsync(It.IsAny<Credentials>()))
            .ReturnsAsync(() => null);

        // Act
        var issueBearerTokenAsync = async () => { await _sut.IssueBearerTokensAsync(It.IsAny<Credentials>()); };

        // Assert
        await issueBearerTokenAsync.Should().ThrowAsync<InvalidCredentialException>();
    }

    [Fact]
    public async Task RefreshBearerTokenAsync_ReturnsRefreshedTokens()
    {
        // Arrange
        var refreshToken = new BearerToken {Value = "t"};
        var newAccessToken = new BearerToken {Value = "a"};
        var newRefreshToken = new BearerToken {Value = "b"};

        _mockJwtBearerManager
            .Setup(x => x.GetPrincipalFromToken(It.IsAny<BearerToken>()))
            .Returns(new ClaimsPrincipal());

        _mockJwtBearerManager
            .Setup(x => x.CreateToken(It.IsAny<IEnumerable<Claim>>(), _mockJwtBearerSettings.Value.AccessToken.LifeSpan))
            .Returns(newAccessToken);

        _mockJwtBearerManager
            .Setup(x => x.CreateToken(It.IsAny<IEnumerable<Claim>>(), _mockJwtBearerSettings.Value.RefreshToken.LifeSpan))
            .Returns(newRefreshToken);

        _mockAuthManager
            .Setup(x => x.AuthenticateAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(_user);

        _mockAuthManager
            .Setup(x => x.GetRefreshTokenAsync(_user))
            .ReturnsAsync(refreshToken);

        // Act
        JwtBearerTokens result = await _sut.RefreshBearerTokensAsync(refreshToken);

        // Assert
        result.AccessToken.Should().BeEquivalentTo("a");
        result.RefreshToken.Should().BeEquivalentTo("b");
        _mockAuthManager.Verify(x => x.StoreRefreshTokenAsync(_user, newRefreshToken));
    }

    [Fact]
    public async Task RefreshBearerTokenAsync_PrincipalCouldNotBeExtracted_ThrowsSecurityTokenException()
    {
        // Arrange
        _mockJwtBearerManager
            .Setup(x => x.GetPrincipalFromToken(It.IsAny<BearerToken>()))
            .Throws<SecurityTokenInvalidLifetimeException>();

        // Act
        var act = async () => { await _sut.RefreshBearerTokensAsync(It.IsAny<BearerToken>()); };

        // Assert
        await act.Should()
            .ThrowAsync<SecurityTokenInvalidLifetimeException>();
    }

    [Fact]
    public async Task RefreshBearerTokenAsync_UserNotFound_ThrowsSecurityTokenException()
    {
        // Arrange
        _mockJwtBearerManager
            .Setup(x => x.GetPrincipalFromToken(It.IsAny<BearerToken>()))
            .Returns(new ClaimsPrincipal());

        _mockAuthManager
            .Setup(x => x.AuthenticateAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(() => null);

        // Act
        var act = async () => { await _sut.RefreshBearerTokensAsync(It.IsAny<BearerToken>()); };

        // Assert
        await act.Should()
            .ThrowAsync<SecurityTokenException>()
            .WithMessage("Could not find the user for the given token.");
    }

    [Fact]
    public async Task RefreshBearerTokenAsync_RefreshTokenNotFound_ThrowsSecurityTokenException()
    {
        // Arrange
        _mockJwtBearerManager
            .Setup(x => x.GetPrincipalFromToken(It.IsAny<BearerToken>()))
            .Returns(new ClaimsPrincipal());

        _mockAuthManager
            .Setup(x => x.AuthenticateAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(_user);

        _mockAuthManager
            .Setup(x => x.GetRefreshTokenAsync(_user))
            .ReturnsAsync(() => null);

        // Act
        var act = async () => { await _sut.RefreshBearerTokensAsync(It.IsAny<BearerToken>()); };

        // Assert
        await act.Should()
            .ThrowAsync<SecurityTokenException>()
            .WithMessage("Invalid refresh token.");
    }

    [Fact]
    public async Task RevokeRefreshTokenAsync_ValidRefreshToken()
    {
        // Arrange
        _mockJwtBearerManager
            .Setup(x => x.GetPrincipalFromToken(It.IsAny<BearerToken>()))
            .Returns(new ClaimsPrincipal());

        _mockAuthManager
            .Setup(x => x.AuthenticateAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(_user);

        // Act
        await _sut.RevokeRefreshTokenAsync(It.IsAny<BearerToken>());

        // Assert
        _mockAuthManager.Verify(x => x.DeleteRefreshTokenAsync(_user));
    }

    [Fact]
    public async Task RevokeRefreshTokenAsync_PrincipalCouldNotBeExtracted_ThrowsSecurityTokenException()
    {
        // Arrange
        _mockJwtBearerManager
            .Setup(x => x.GetPrincipalFromToken(It.IsAny<BearerToken>()))
            .Throws<SecurityTokenInvalidSignatureException>();

        // Act
        var act = async () => { await _sut.RevokeRefreshTokenAsync(It.IsAny<BearerToken>()); };

        // Assert
        await act.Should()
            .ThrowAsync<SecurityTokenInvalidSignatureException>();
    }

    [Fact]
    public async Task RevokeRefreshTokenAsync_UserNotFound_ThrowsSecurityTokenException()
    {
        // Arrange
        _mockJwtBearerManager
            .Setup(x => x.GetPrincipalFromToken(It.IsAny<BearerToken>()))
            .Returns(new ClaimsPrincipal());

        _mockAuthManager
            .Setup(x => x.AuthenticateAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(() => null);

        // Act
        var act = async () => { await _sut.RevokeRefreshTokenAsync(It.IsAny<BearerToken>()); };

        // Assert
        await act.Should()
            .ThrowAsync<SecurityTokenException>()
            .WithMessage("Could not find the user for the given token.");
    }
}