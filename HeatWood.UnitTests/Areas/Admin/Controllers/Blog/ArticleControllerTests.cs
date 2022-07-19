using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using HeatWood.Areas.Admin.Controllers.Blog;
using HeatWood.Entities.Blog;
using HeatWood.Exceptions;
using HeatWood.Models.Blog;
using HeatWood.Services.Blog;
using HeatWood.ValueObjects;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Moq;

namespace HeatWood.UnitTests.Areas.Admin.Controllers.Blog;

public sealed class ArticleControllerTests
{
    private readonly Mock<IArticleService> _mockArticleService;

    private readonly ArticleController _sut;

    private readonly Mock<IValidator<ArticleModel>> _mockArticleModelValidator;

    public ArticleControllerTests()
    {
        _mockArticleService = new Mock<IArticleService>();
        _mockArticleModelValidator = new Mock<IValidator<ArticleModel>>();

        _sut = new ArticleController(_mockArticleService.Object, _mockArticleModelValidator.Object);
    }

    [Fact]
    public async Task Create_ArticleValidData_Returns201StatusCode()
    {
        // Arrange
        var model = new ArticleModel
        {
            Published = true,
            Translations = new HashSet<ArticleModel.TranslationModel>(new[]
            {
                new ArticleModel.TranslationModel
                {
                    Locale = "uk",
                    Title = "X",
                    Description = "Y"
                }
            })
        };

        _mockArticleService
            .Setup(x => x.CreateAsync(model))
            .Callback(() => model.Id = Guid.Parse("bcc66d2b-29cf-4c63-ad47-0eec80f19d6d"));

        _mockArticleModelValidator
            .Setup(x => x.ValidateAsync(model, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(Array.Empty<ValidationFailure>()));

        // Act
        var result = await _sut.Create(model) as CreatedAtActionResult;

        // Assert
        result!.RouteValues!["id"].Should().BeEquivalentTo(Guid.Parse("bcc66d2b-29cf-4c63-ad47-0eec80f19d6d"));
    }

    [Fact]
    public async Task Create_ArticleWithInvalidData_Returns400StatusCode()
    {
        _mockArticleModelValidator
            .Setup(x => x.ValidateAsync(It.IsAny<ArticleModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(new ValidationFailure[] {new("Published", "Error")}));

        var result = (BadRequestObjectResult) await _sut.Create(It.IsAny<ArticleModel>());
        var resultValue = (SerializableError) result.Value!;

        resultValue["Published"].As<string[]>()[0].Should().Be("Error");
    }

    [Fact]
    public async Task GetById_ReturnsTheArticleModel()
    {
        var articleId = new Guid("A4F0295B-CC2B-4539-B261-BBEF0165373E");
        _mockArticleService
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new Article
            {
                Id = articleId,
                Published = true,
                Translations =
                {
                    new Article.Translation
                    {
                        ArticleId = articleId,
                        Locale = "en",
                        Title = "T",
                        Description = "D",
                        Slug = new Slug("s")
                    }
                }
            });

        var result = (OkObjectResult) await _sut.GetById(It.IsAny<Guid>());
        var model = (ArticleModel) result.Value!;

        model.Id.Should().Be(articleId);
        model.Published.Should().Be(true);

        ArticleModel.TranslationModel translation = model.Translations.First();
        translation.Locale.Should().Be("en");
        translation.Title.Should().Be("T");
        translation.Description.Should().Be("D");
        translation.Slug.Should().Be("s");
    }

    [Fact]
    public async Task GetById_Returns404Status()
    {
        _mockArticleService
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ThrowsAsync(new RecordNotFoundException());

        var _ = (NotFoundResult) await _sut.GetById(It.IsAny<Guid>());
    }

    [Fact]
    public async Task Delete_DeletesArticle()
    {
        var _ = (NoContentResult) await _sut.Delete(It.IsAny<Guid>());

        _mockArticleService.Verify(x => x.DeleteAsync(It.IsAny<Guid>()));
    }

    [Fact]
    public async Task Delete_WithInvalidId_Returns404StatusCode()
    {
        _mockArticleService
            .Setup(x => x.DeleteAsync(It.IsAny<Guid>()))
            .ThrowsAsync(new RecordNotFoundException());

        var _ = (NotFoundResult) await _sut.Delete(It.IsAny<Guid>());
    }

    [Fact]
    public async Task Update_UpdatesArticle()
    {
        _mockArticleModelValidator
            .Setup(x => x.ValidateAsync(It.IsAny<ValidationContext<ArticleModel>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(Array.Empty<ValidationFailure>()));

        var _ = (NoContentResult) await _sut.Update(It.IsAny<Guid>(), new ArticleModel());

        _mockArticleService.Verify(x => x.UpdateAsync(It.IsAny<ArticleModel>()));
    }

    [Fact]
    public async Task Update_WithInvalidArticleModel_Returns400StatusCode()
    {
        _mockArticleModelValidator
            .Setup(x => x.ValidateAsync(It.IsAny<ValidationContext<ArticleModel>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(new[] {new ValidationFailure("Published", "Error")}));

        var result = (BadRequestObjectResult) await _sut.Update(It.IsAny<Guid>(), new ArticleModel());
        var resultValue = (SerializableError) result.Value!;

        resultValue["Published"].As<string[]>()[0].Should().Be("Error");
    }
}