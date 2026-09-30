using CivicConnect.Core.Services;
using CivicConnect.Web.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace CivicConnect.Web.Controllers;

[Route("api/v1/feedback")]
public class FeedbackController(CurrentUserAccessor accessor, FeedbackService feedback)
    : ApiControllerBase(accessor)
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var user = await UserAsync(ct);
        return Ok(await feedback.ListAsync(user, ct));
    }

    [HttpPost("{id:long}/read")]
    public async Task<IActionResult> MarkRead(long id, CancellationToken ct)
    {
        var user = await UserAsync(ct);
        await feedback.MarkReadAsync(user, id, ct);
        return NoContent();
    }
}
