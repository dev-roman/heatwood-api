using FluentAssertions;
using HeatWood.Database;
using HeatWood.Entities.Blog;
using HeatWood.Exceptions;
using HeatWood.Models.Blog;
using HeatWood.Services;
using HeatWood.Services.Blog;
using HeatWood.UnitTests.Stubs;
using HeatWood.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace HeatWood.UnitTests.Services.Blog;

public sealed class ArticleServiceTests
{
    private readonly ArticleService _sut;

    private readonly Mock<IGuidService> _mockGuidService;

    private readonly HeatWoodDbContextStub _dbContext;

    private readonly Guid _articleGuid = new("172F8B5E-9EF7-47A6-824F-954A564BAA03");

    public ArticleServiceTests()
    {
        DbContextOptions dbContextOptions = new DbContextOptionsBuilder<HeatWoodDbContext>()
            .UseInMemoryDatabase("HeatWood")
            .Options;
        _dbContext = new HeatWoodDbContextStub(dbContextOptions);
        _mockGuidService = new Mock<IGuidService>();
        _sut = new ArticleService(_dbContext, _mockGuidService.Object);

        var article = new Article
        {
            Id = _articleGuid,
            Published = true,
            CreatedAt = DateTime.Parse("2022-07-19"),
            Translations =
            {
                new Article.Translation
                {
                    ArticleId = _articleGuid,
                    Locale = "en",
                    Title = "T",
                    Description = "D",
                    Slug = new Slug("s")
                }
            }
        };

        _dbContext.Articles.Add(article);
        _dbContext.SaveChanges();
    }

    [Fact]
    public async Task CreateAsync_SuccessfullyCreateAnArticle()
    {
        var articleGuid = new Guid("63C4A3E8-E18D-43E3-AAA9-E68E20764645");
        _mockGuidService.Setup(x => x.NewGuid())
            .Returns(articleGuid);

        var model = new ArticleModel
        {
            Published = true,
            Categories = new HashSet<Guid>
            {
                new("B98F3752-C576-44C7-8E5A-2F37682D4C8A") // Not existing category id
            },
            Translations = new()
            {
                new ArticleModel.TranslationModel
                {
                    Locale = "en",
                    Title = "T",
                    Description = "D",
                    Slug = "s"
                }
            }
        };

        await _sut.CreateAsync(model);

        Article? storedArticle = _dbContext.Articles
            .Include(a => a.ArticleCategories)
            .FirstOrDefault(a => a.Id == articleGuid);
        storedArticle.Should().NotBeNull();

        storedArticle!.Published.Should().BeTrue();

        Article.Translation? translation = storedArticle.Translations.FirstOrDefault();
        translation.Should().NotBeNull();

        translation!.Locale.Should().Be("en");
        translation.Title.Should().Be("T");
        translation.Description.Should().Be("D");
        translation.Slug.Should().Be(new Slug("s"));
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsTheArticle()
    {
        Article article = await _sut.GetByIdAsync(_articleGuid);

        article.Id.Should().Be(_articleGuid);

        Article.Translation? translation = article.Translations.FirstOrDefault();
        translation.Should().NotBeNull();
        translation!.Title.Should().Be("T");
        translation.Description.Should().Be("D");
        translation.Locale.Should().Be("en");
        translation.Slug.Should().Be(new Slug("s"));
    }

    [Fact]
    public async Task GetByIdAsync_ProvidedWithNotExistingArticleId_ThrowsRecordNotFoundException()
    {
        var getByIdAsync = async () => await _sut.GetByIdAsync(new Guid("517E8C3D-005E-4FBE-B061-BADCD702E2B2"));

        await getByIdAsync.Should().ThrowAsync<RecordNotFoundException>();
    }
    
    [Fact]
    public async Task UpdateAsync_UpdatesExistingModel()
    {
        // Arrange
        var model = new ArticleModel
        {
            Id = _articleGuid,
            Published = false,
            Translations = new()
            {
                new ArticleModel.TranslationModel
                {
                    Locale = "en",
                    Title = "T1",
                    Description = "D1",
                    Slug = "s1"
                },
                new ArticleModel.TranslationModel
                {
                    Locale = "uk",
                    Title = "T2",
                    Description = "D2",
                    Slug = "s2"
                }
            }
        };

        // Act
        await _sut.UpdateAsync(model);
        
        // Assert
        Article? article = await _dbContext.Articles.FindAsync(_articleGuid);
        article.Should().NotBeNull();

        article!.Published.Should().Be(false);
        var translationEn = article.Translations.First(t => t.Locale == "en");
        var translationUk = article.Translations.First(t => t.Locale == "uk");

        translationEn.Title.Should().Be("T1");
        translationEn.Description.Should().Be("D1");
        translationEn.Slug.Should().Be(new Slug("s1"));
        
        translationUk.Title.Should().Be("T2");
        translationUk.Description.Should().Be("D2");
        translationUk.Slug.Should().Be(new Slug("s2"));
    }
    
    [Fact]
    public async Task UpdateAsync_ProvidedWithModelWithInvalidId_ThrowsRecordNotFoundException()
    {
        // Arrange
        var notExistingArticleId = new Guid("3B1C5758-40D4-469A-8F0C-D77469921DA5");
        var model = new ArticleModel
        {
            Id = notExistingArticleId
        };

        // Act
        var updateAsync = async () => await _sut.UpdateAsync(model);

        // Assert
        await updateAsync.Should().ThrowAsync<RecordNotFoundException>();
    }
    
    [Fact]
    public async Task UpdateAsync_ProvidedWithModelWithoutId_ThrowsArgumentNullException()
    {
        // Arrange
        var model = new ArticleModel();
        
        // Act
        var updateAsync = async () => await _sut.UpdateAsync(model);

        // Assert
        await updateAsync.Should().ThrowAsync<ArgumentNullException>();
    }
    
    [Fact]
    public async Task DeleteAsync_DeletesArticleById()
    {
        await _sut.DeleteAsync(_articleGuid);

        var dbArticle = await _dbContext.Articles.FindAsync(_articleGuid);

        dbArticle.Should().BeNull();
    }
    
    [Fact]
    public async Task DeleteAsync_WithInvalidId_ThrowsRecordNotFoundException()
    {
        var deleteAsync = async () => await _sut.DeleteAsync(new Guid("535C9C8B-C17E-489A-A94B-B9103E30DB31"));

        await deleteAsync.Should().ThrowAsync<RecordNotFoundException>();
    }
}