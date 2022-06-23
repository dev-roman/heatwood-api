namespace HeatWood.Models;

public record JwtBearerSettings
{
    public string Secret { get; init; } = string.Empty;

    public TokenSettings AccessToken { get; init; } = new();

    public TokenSettings RefreshToken { get; init; } = new();

    public record TokenSettings
    {
        public TimeSpan LifeSpan { get; init; } = default;
    }
}