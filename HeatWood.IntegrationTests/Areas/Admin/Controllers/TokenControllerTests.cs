using System.Net;
using FluentAssertions;
using HeatWood.Models;
using HeatWood.Models.Auth;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace HeatWood.IntegrationTests.Areas.Admin.Controllers;

public sealed class TokenControllerTests : IntegrationTests
{
    [Fact]
    public async Task Generate_ReturnsTokens()
    {
        JwtBearerTokens tokens = await GenerateTokensAsync();

        tokens.AccessToken.Should().NotBeEmpty();
        tokens.RefreshToken.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData("admin", "invalid_pass")]
    [InlineData("invalid_username", "invalid_pass")]
    [InlineData("invalid_username", "valid_password")]
    public async Task Generate_WithNotExistingUserCredentials_Returns401StatusCode(string userName, string password)
    {
        HttpResponseMessage response = await HttpClient.PostAsync("api/admin/token/generate", SerializeRequest(
            new Credentials
            {
                UserName = userName,
                Password = password
            }));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("admin", "")]
    [InlineData("", "")]
    [InlineData("", "valid_password")]
    public async Task Generate_WithInvalidCredentials_Returns400StatusCode(string userName, string password)
    {
        HttpResponseMessage response = await HttpClient.PostAsync("api/admin/token/generate", SerializeRequest(
            new Credentials
            {
                UserName = userName,
                Password = password
            }));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Refresh_ReturnsRefreshedTokens()
    {
        JwtBearerTokens tokens = await GenerateTokensAsync();

        HttpResponseMessage response = await RefreshTokensAsync(tokens.RefreshToken);
        var refreshedTokens = await DeserializeResponseAsync<JwtBearerTokens>(response);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        refreshedTokens.AccessToken.Should().NotBeEmpty();
        refreshedTokens.RefreshToken.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Refresh_WithInvalidRefreshToken_Returns400StatusCode()
    {
        HttpResponseMessage refreshResponse = await RefreshTokensAsync(null);

        refreshResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Refresh_WithExpiredRefreshToken_Returns401StatusCode()
    {
        // Arrange
        HttpClient client = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.Configure<JwtBearerSettings>(opts =>
                    {
                        opts.AccessToken.LifeSpan = TimeSpan.FromHours(-1);
                        opts.RefreshToken.LifeSpan = TimeSpan.FromHours(-1);
                    });
                });
            }).CreateClient();

        HttpResponseMessage response = await client.PostAsync("api/admin/token/generate", SerializeRequest(LoginCredentials));
        var expiredTokens = await DeserializeResponseAsync<JwtBearerTokens>(response);

        // Act
        HttpResponseMessage refreshResponse = await RefreshTokensAsync(expiredTokens.RefreshToken);

        // Assert
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Revoke_Returns204StatusCode()
    {
        JwtBearerTokens tokens = await GenerateTokensAsync();

        HttpResponseMessage response = await RevokeTokensAsync(tokens.RefreshToken);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Refresh_WithRevokedRefreshToken_Returns401StatusCode()
    {
        JwtBearerTokens tokens = await GenerateTokensAsync();
        await RevokeTokensAsync(tokens.RefreshToken);

        HttpResponseMessage response = await RefreshTokensAsync(tokens.RefreshToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Revoke_Failure_BadRequest()
    {
        HttpResponseMessage response = await RevokeTokensAsync(null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}