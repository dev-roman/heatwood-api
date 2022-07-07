using System.ComponentModel.DataAnnotations;

namespace HeatWood.Models;

public sealed class Credentials
{
    [Required]
    public string UserName { get; init; } = string.Empty;
    
    [Required]
    public string Password { get; init; } = string.Empty;
}