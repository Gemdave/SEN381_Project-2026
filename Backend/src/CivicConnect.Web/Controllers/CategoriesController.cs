using CivicConnect.Core.Services;
using CivicConnect.Web.Dtos;
using CivicConnect.Web.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace CivicConnect.Web.Controllers;

[Route("api/v1/categories")]
public class CategoriesController(CurrentUserAccessor accessor, CategoryService categories)
    : ApiControllerBase(accessor)
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        await UserAsync(ct);
        var list = await categories.ListActiveAsync(ct);
        return Ok(list.Select(c => new CategoryView(c.Id, c.Name)));
    }
}
