using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace HeatWood.IntegrationTests;

public class IntegrationTests
{
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
}