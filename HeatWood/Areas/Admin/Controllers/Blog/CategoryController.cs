using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;

namespace HeatWood.Areas.Admin.Controllers.Blog;

[ApiController]
[Route("api/admin/blog/[controller]")]
[Consumes(MediaTypeNames.Application.Json)]
[Produces(MediaTypeNames.Application.Json)]
public sealed class CategoryController : ControllerBase
{
    
}