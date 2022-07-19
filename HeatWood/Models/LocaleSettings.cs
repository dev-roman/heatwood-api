namespace HeatWood.Models;

public sealed class LocaleSettings
{
    private readonly string[] _supportedLocales = Array.Empty<string>();

    public string[] SupportedLocales
    {
        get => _supportedLocales;
        init
        {
            if (!AreLocalesValid(value))
            {
                throw new ArgumentException("Invalid locales provided,");
            }

            _supportedLocales = value;
        }
    }

    private readonly string _fallbackLocale = string.Empty;

    public string FallbackLocale
    {
        get => _fallbackLocale;
        init
        {
            if (!IsLocaleValid(value))
            {
                throw new ArgumentException("Invalid fallback locale provided.");
            }

            _fallbackLocale = value;
        }
    }

    private static bool AreLocalesValid(string[] locales)
    {
        return locales.Length != 0 && locales.All(IsLocaleValid);
    }

    private static bool IsLocaleValid(string locale)
    {
        return !string.IsNullOrWhiteSpace(locale);
    }
}