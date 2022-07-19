using System.Net.Mime;
using FluentValidation;
using FluentValidation.AspNetCore;
using FluentValidation.Results;
using HeatWood.Entities.Blog;
using HeatWood.Exceptions;
using HeatWood.Models.Blog;
using HeatWood.Services.Blog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HeatWood.Areas.Admin.Controllers.Blog;

[ApiController]
[Route("api/admin/blog/[controller]")]
[Authorize]
[Consumes(MediaTypeNames.Application.Json)]
[Produces(MediaTypeNames.Application.Json)]
public sealed class ArticleController : ControllerBase
{
    private readonly IArticleService _service;

    private readonly IValidator<ArticleModel> _validator;

    public ArticleController(IArticleService service, IValidator<ArticleModel> validator)
    {
        _service = service;
        _validator = validator;
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] ArticleModel model)
    {
        ValidationResult result = await _validator.ValidateAsync(model);
        if (!result.IsValid)
        {
            result.AddToModelState(ModelState, string.Empty);
            return BadRequest(ModelState);
        }

        await _service.CreateAsync(model);

        return CreatedAtAction(nameof(GetById), new {id = model.Id}, null);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ArticleModel))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            Article article = await _service.GetByIdAsync(id);
            ArticleModel articleModel = ArticleModel.FromArticle(article);

            return Ok(articleModel);
        }
        catch (RecordNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpGet("list")]
    // TODO: implement filter model
    public async Task<IActionResult> List()
    {
        var articles = await _service.ListAsync();
        var viewModels = articles.Select(ArticleModel.FromArticle);

        return Ok(viewModels);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await _service.DeleteAsync(id);
            return NoContent();
        }
        catch (RecordNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] ArticleModel model)
    {
        var validationContext = new ValidationContext<ArticleModel>(model);
        validationContext.RootContextData[ArticleModel.FluentValidationRootContextDataGuidKey] = id.ToString();
        ValidationResult result = await _validator.ValidateAsync(validationContext);
        if (!result.IsValid)
        {
            result.AddToModelState(ModelState, string.Empty);
            return BadRequest(ModelState);
        }

        model.Id = id;
        await _service.UpdateAsync(model);

        return NoContent();
    }
}