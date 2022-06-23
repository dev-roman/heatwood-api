using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace HeatWood.Models;

public record BearerToken
{
    private readonly string _value = string.Empty;

    [Required]
    [FromHeader(Name = "Authorization")]
    public string Value
    {
        get => _value;
        init => _value = Normalize(value);
    }
    
    private static string Normalize(string token)
    {
        token = token.Trim();
        return token.StartsWith("Bearer ") ? token[7..] : token;
    }
}