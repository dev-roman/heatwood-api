namespace HeatWood.Entities.Blog;

public sealed class Category
{
    public const short TitleMaxLength = 60;
    
    private HashSet<Translation> _translations = new();
    
    public Guid Id { get; set; }

    public IEnumerable<Translation> Translations => _translations;

    public ICollection<ArticleCategory> ArticleCategories { get; set; }
    
    public sealed class Translation
    {
        public Guid CategoryId { get; set; }

        public string Locale { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;
    }
}