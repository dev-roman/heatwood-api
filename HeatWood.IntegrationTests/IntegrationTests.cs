using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using HeatWood.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.Testing;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace HeatWood.IntegrationTests;

public class IntegrationTests
{
    protected readonly Credentials LoginCredentials = new()
    {
        UserName = "admin",
        Password = "valid_password"
    };

    protected readonly HttpClient HttpClient;

    protected IntegrationTests()
    {
        var appFactory = new WebApplicationFactory<Program>();
        HttpClient = appFactory.CreateClient();
    }

    protected static async Task<T> DeserializeResponseAsync<T>(HttpResponseMessage response)
    {
        var responseText = await response.Content.ReadAsStringAsync();

        return JsonSerializer.Deserialize<T>(responseText, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        })!;
    }

    protected static StringContent SerializeRequest(object request)
    {
        var json = JsonConvert.SerializeObject(request);
        return new StringContent(json, Encoding.UTF8, "application/json");
    }

    protected async Task<JwtBearerTokens> GenerateTokensAsync()
    {
        HttpResponseMessage response =
            await HttpClient.PostAsync("api/token/generate", SerializeRequest(LoginCredentials));

        return await DeserializeResponseAsync<JwtBearerTokens>(response);
    }

    protected async Task<HttpResponseMessage> RefreshTokensAsync(string? refreshToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "api/token/refresh");
        request.Headers.Authorization =
            new AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, refreshToken);

        return await HttpClient.SendAsync(request);
    }

    protected async Task<HttpResponseMessage> RevokeTokensAsync(string? refreshToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "api/token/revoke");
        request.Headers.Authorization =
            new AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, refreshToken);

        return await HttpClient.SendAsync(request);
    }
}