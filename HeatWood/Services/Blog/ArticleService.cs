using HeatWood.Database;
using HeatWood.Entities.Blog;
using HeatWood.Exceptions;
using HeatWood.Models.Blog;
using Microsoft.EntityFrameworkCore;

namespace HeatWood.Services.Blog;

public sealed class ArticleService : IArticleService
{
    private readonly HeatWoodDbContext _context;

    private readonly IGuidService _guidService;

    public ArticleService(HeatWoodDbContext context, IGuidService guidService)
    {
        _context = context;
        _guidService = guidService;
    }

    public async Task CreateAsync(ArticleModel model)
    {
        var article = new Article
        {
            Id = _guidService.NewGuid(),
            Published = model.Published,
            CreatedAt = DateTime.Now
        };

        var categories =
            await (
                from category in _context.Categories
                where model.Categories.Contains(category.Id)
                select category
            ).ToArrayAsync();

        article.ArticleCategories = categories.Select(c => new ArticleCategory
        {
            Article = article,
            Category = c
        }).ToArray();

        foreach (var translation in model.Translations)
        {
            article.AddOrUpdateTranslation(translation.Locale, translation.Title, translation.Description,
                translation.Slug);
        }

        _context.Articles.Add(article);
        await _context.SaveChangesAsync();

        model.Id = article.Id;
    }

    public async Task<Article> GetByIdAsync(Guid id)
    {
        Article? article = await _context.Articles
            .Include(a => a.ArticleCategories)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (article is null)
        {
            throw new RecordNotFoundException();
        }

        return article;
    }

    public async Task<IEnumerable<Article>> ListAsync()
    {
        return await _context.Articles.ToListAsync();
    }

    public async Task UpdateAsync(ArticleModel model)
    {
        if (model.Id is null)
        {
            throw new ArgumentNullException(nameof(model.Id));
        }

        Article? article = await _context.Articles.FirstOrDefaultAsync(a => a.Id == model.Id);
        if (article is null)
        {
            throw new RecordNotFoundException();
        }

        article.Published = model.Published;
        article.UpdatedAt = DateTime.Now;

        foreach (var translation in model.Translations)
        {
            article.AddOrUpdateTranslation(translation.Locale, translation.Title, translation.Description,
                translation.Slug);
        }

        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        Article? article = await _context.Articles.FirstOrDefaultAsync(a => a.Id == id);
        if (article is null)
        {
            throw new RecordNotFoundException();
        }

        _context.Articles.Remove(article);

        await _context.SaveChangesAsync();
    }
}