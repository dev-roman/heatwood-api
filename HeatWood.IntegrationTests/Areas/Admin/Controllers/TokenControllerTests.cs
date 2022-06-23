using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
using HeatWood.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HeatWood.IntegrationTests.Areas.Admin.Controllers;

public sealed class TokenControllerTests : IntegrationTests
{
    private readonly Credentials _loginCredentials = new()
    {
        UserName = "admin",
        Password = "valid_password"
    };

    [Fact]
    public async Task Generate_Success()
    {
        HttpResponseMessage response =
            await HttpClient.PostAsync("api/token/generate", SerializeRequest(_loginCredentials));
        var bearerTokens = await DeserializeResponseAsync<JwtBearerTokens>(response);

        bearerTokens.AccessToken.Should().NotBeEmpty();
        bearerTokens.RefreshToken.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData("admin", "invalid_pass")]
    [InlineData("invalid_username", "invalid_pass")]
    [InlineData("invalid_username", "valid_password")]
    public async Task Generate_Failure_Unauthorized(string userName, string password)
    {
        HttpResponseMessage response = await HttpClient.PostAsync("api/token/generate", SerializeRequest(
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
    public async Task Generate_Failure_BadRequest(string userName, string password)
    {
        HttpResponseMessage response = await HttpClient.PostAsync("api/token/generate", SerializeRequest(
            new Credentials
            {
                UserName = userName,
                Password = password
            }));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Refresh_Success()
    {
        HttpResponseMessage generateResponse =
            await HttpClient.PostAsync("api/token/generate", SerializeRequest(_loginCredentials));
        var generatedTokens = await DeserializeResponseAsync<JwtBearerTokens>(generateResponse);

        var requestMessage = new HttpRequestMessage(HttpMethod.Post, "api/token/refresh");
        requestMessage.Headers.Authorization =
            new AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, generatedTokens.RefreshToken);

        // Act
        HttpResponseMessage refreshResponse = await HttpClient.SendAsync(requestMessage);
        var refreshedTokens = await DeserializeResponseAsync<JwtBearerTokens>(refreshResponse);

        // Assert
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        refreshedTokens.AccessToken.Should().NotBeEmpty();
        refreshedTokens.RefreshToken.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Refresh_Failure_BadRequest()
    {
        HttpResponseMessage refreshResponse = await HttpClient.PostAsync("api/token/refresh", null);

        refreshResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Refresh_Failure_Unauthorized()
    {
        // Arrange
        // TODO: extract to separate class/method
        HttpClient expiredTokensClient = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.AddSingleton<IOptions<JwtBearerSettings>>(_ => new OptionsWrapper<JwtBearerSettings>(
                        new JwtBearerSettings
                        {
                            Secret = "some_secret_secret",
                            AccessToken = new() {LifeSpan = TimeSpan.FromMinutes(-1)},
                            RefreshToken = new() {LifeSpan = TimeSpan.FromMinutes(-1)}
                        }));
                });
            }).CreateClient();

        HttpResponseMessage response =
            await expiredTokensClient.PostAsync("api/token/generate", SerializeRequest(_loginCredentials));
        var expiredBearerTokens = await DeserializeResponseAsync<JwtBearerTokens>(response);
        
        var requestMessage = new HttpRequestMessage(HttpMethod.Post, "api/token/refresh");
        requestMessage.Headers.Authorization =
            new AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, expiredBearerTokens.RefreshToken);

        // Act
        HttpResponseMessage refreshResponse = await HttpClient.SendAsync(requestMessage);
        
        // Assert
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
    
    [Fact]
    public async Task Revoke_Success()
    {
        // Arrange
        HttpResponseMessage response =
            await HttpClient.PostAsync("api/token/generate", SerializeRequest(_loginCredentials));
        var bearerTokens = await DeserializeResponseAsync<JwtBearerTokens>(response);
        
        // Act
        var requestMessage = new HttpRequestMessage(HttpMethod.Post, "api/token/revoke");
        requestMessage.Headers.Authorization =
            new AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, bearerTokens.RefreshToken);
        HttpResponseMessage revokeResponse = await HttpClient.SendAsync(requestMessage);

        // Assert
        revokeResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        
        var refreshRequestMessage = new HttpRequestMessage(HttpMethod.Post, "api/token/refresh");
        refreshRequestMessage.Headers.Authorization =
            new AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, bearerTokens.RefreshToken);
        HttpResponseMessage refreshResponse = await HttpClient.SendAsync(refreshRequestMessage);

        refreshResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
    
    [Fact]
    public async Task Revoke_Failure_Unauthorized()
    {
        // Arrange
        HttpResponseMessage response =
            await HttpClient.PostAsync("api/token/generate", SerializeRequest(_loginCredentials));
        var bearerTokens = await DeserializeResponseAsync<JwtBearerTokens>(response);
        
        // Act
        var requestMessage = new HttpRequestMessage(HttpMethod.Post, "api/token/revoke");
        requestMessage.Headers.Authorization =
            new AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, bearerTokens.RefreshToken);
        HttpResponseMessage revokeResponse = await HttpClient.SendAsync(requestMessage);

        // Assert
        revokeResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        
        var refreshRequestMessage = new HttpRequestMessage(HttpMethod.Post, "api/token/refresh");
        refreshRequestMessage.Headers.Authorization =
            new AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, bearerTokens.RefreshToken);
        HttpResponseMessage refreshResponse = await HttpClient.SendAsync(refreshRequestMessage);

        refreshResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
    
    [Fact]
    public async Task Revoke_Failure_BadRequest()
    {
        HttpResponseMessage revokeResponse = await HttpClient.PostAsync("api/token/revoke", null);

        revokeResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}