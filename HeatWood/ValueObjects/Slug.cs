using System.Text;

namespace HeatWood.ValueObjects;

public record Slug
{
    public const short MaxLength = 120;
    
    public string Value { get; private init; }
    
    public Slug(string slug)
    {
        if (string.IsNullOrEmpty(slug))
        {
            throw new ArgumentException(nameof(slug));
        }

        Value = Sanitize(slug);
    }

    private static string Sanitize(string slug)
    {
        var stringBuilder = new StringBuilder();
        var previousCharWasSpace = false;
        
        foreach (var c in slug.Trim())
        {
            if (c == ' ')
            {
                if (!previousCharWasSpace)
                {
                    stringBuilder.Append(c);
                }

                previousCharWasSpace = true;
            }
            else
            {
                previousCharWasSpace = false;
                stringBuilder.Append(c);
            }
        }

        return stringBuilder.ToString().ToLower();
    }
}