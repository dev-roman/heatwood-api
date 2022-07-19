using HeatWood.ValueObjects;

namespace HeatWood.Entities.Blog;

public sealed class Article
{
    public const short TitleMaxLength = 120;

    private readonly HashSet<Translation> _translations = new();

    public Guid Id { get; init; }

    public bool Published { get; set; }

    public DateTime CreatedAt { get; init; }

    public DateTime? UpdatedAt { get; set; }

    public ICollection<Translation> Translations => _translations;

    public ICollection<ArticleCategory> ArticleCategories { get; set; } = new List<ArticleCategory>();

    public void AddOrUpdateTranslation(string locale, string title, string description, string slug)
    {
        Translation? translation = _translations.FirstOrDefault(t => t.Locale == locale);

        if (translation is not null)
        {
            translation.Title = title;
            translation.Description = description;
            translation.Slug = new Slug(slug);

            return;
        }

        _translations.Add(new Translation
        {
            ArticleId = Id,
            Locale = locale,
            Title = title,
            Description = description,
            Slug = new Slug(slug)
        });
    }

    public sealed class Translation
    {
        private string _title = string.Empty;

        public Guid ArticleId { get; init; }

        public string Locale { get; init; } = null!;

        public string Title
        {
            get => _title;
            set => _title = char.ToUpper(value[0]) + value[1..];
        }

        public string Description { get; set; } = string.Empty;

        public Slug Slug { get; set; } = null!;

        #region Overrides of Object

        public override bool Equals(object? obj)
        {
            return obj is Translation t && t.ArticleId == ArticleId && t.Locale == Locale;
        }

        public override int GetHashCode()
        {
            return ArticleId.GetHashCode() ^ Locale.GetHashCode();
        }

        #endregion
    }
}