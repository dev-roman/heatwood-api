using HeatWood.Entities.Blog;
using HeatWood.Models.Blog;

namespace HeatWood.Services.Blog;

public interface IArticleService
{
    Task CreateAsync(ArticleModel model);

    Task<Article> GetByIdAsync(Guid id);
    
    Task<IEnumerable<Article>> ListAsync();

    Task UpdateAsync(ArticleModel model);
    
    Task DeleteAsync(Guid id);
}