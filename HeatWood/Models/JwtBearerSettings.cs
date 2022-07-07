namespace HeatWood.Models;

public record JwtBearerSettings
{
    public string Secret { get; set; } = string.Empty;

    public TokenSettings AccessToken { get; set; } = new();

    public TokenSettings RefreshToken { get; set; } = new();

    public record TokenSettings
    {
        public TimeSpan LifeSpan { get; set; }
    }
}