using FluentValidation;
using HeatWood.Database;
using HeatWood.Entities.Blog;
using HeatWood.Models;
using HeatWood.Models.Blog;
using HeatWood.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HeatWood.Validators;

public sealed class ArticleModelValidator : AbstractValidator<ArticleModel>
{
    public ArticleModelValidator(IOptions<LocaleSettings> localeSettings, HeatWoodDbContext dbContext)
    {
        RuleFor(a => a.Published).NotEmpty();

        RuleFor(a => a.Translations)
            .Must(translationModels => translationModels.Select(t => t.Slug).Count() == translationModels.Select(t => t.Slug).Distinct().Count())
            .WithMessage("Duplicate slug");
        
        RuleFor(a => a.Translations)
            .Must(translationModels => translationModels.Select(t => t.Locale).Count() == translationModels.Select(t => t.Locale).Distinct().Count())
            .WithMessage("Duplicate locale");
        
        RuleForEach(a => a.Translations)
            .ChildRules(v =>
            {
                v.RuleFor(t => t.Locale)
                    .NotEmpty()
                    .Must(locale => localeSettings.Value.SupportedLocales.Contains(locale))
                    .WithMessage(
                        $"The Locale field must contain one of the following values: {string.Join(',', localeSettings.Value.SupportedLocales)}"); // TODO: move the translation to the appropriate place

                v.RuleFor(t => t.Title)
                    .NotEmpty()
                    .Length(1, Article.TitleMaxLength);

                v.RuleFor(t => t.Slug)
                    .NotEmpty()
                    .Length(1, Slug.MaxLength)
                    .MustAsync(async (translationModel, slug, context, cancellationToken) =>
                    {
                        if (!context.RootContextData.ContainsKey(ArticleModel.FluentValidationRootContextDataGuidKey))
                        {
                            return !await dbContext.Articles
                                .Where(a => a.Translations.Any(t => t.Slug == new Slug(slug)))
                                .AnyAsync(cancellationToken);
                        }

                        Guid articleId =
                            Guid.Parse((string) context.RootContextData[
                                ArticleModel.FluentValidationRootContextDataGuidKey]);

                        return !await dbContext.Articles
                            .Where(a => a.Translations.Any(t =>
                                !t.ArticleId.Equals(articleId) &&
                                t.Slug == new Slug(slug)
                            ))
                            .AnyAsync(cancellationToken);
                    })
                    .WithMessage("Slug must be unique.");
            });
    }
}