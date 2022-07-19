using HeatWood.Entities.Blog;
using Swashbuckle.AspNetCore.Annotations;

namespace HeatWood.Models.Blog;

public class ArticleModel
{
    public const string FluentValidationRootContextDataGuidKey = "ArticleGuidString";
    
    [SwaggerSchema(ReadOnly = true)]
    public Guid? Id { get; set; }
    
    public bool Published { get; init; }

    public HashSet<Guid> Categories { get; init; } = new();

    public HashSet<TranslationModel> Translations { get; init; } = new();

    public static ArticleModel FromArticle(Article article)
    {
        return new ArticleModel
        {
            Id = article.Id,
            Published = article.Published,
            Categories = article.ArticleCategories.Select(ac => ac.CategoryId).ToHashSet(),
            Translations = article.Translations.Select(TranslationModel.FromArticleTranslation).ToHashSet()
        };
    }

    public sealed class TranslationModel
    {
        public string Locale { get; init; } = string.Empty;

        public string Title { get; init; } = string.Empty;

        public string Description { get; init; } = string.Empty;
        
        public string Slug { get; init; } = string.Empty;

        public static TranslationModel FromArticleTranslation(Article.Translation translation)
        {
            return new TranslationModel
            {
                Locale = translation.Locale,
                Title = translation.Title,
                Description = translation.Description,
                Slug = translation.Slug.Value
            };
        }
    }
}