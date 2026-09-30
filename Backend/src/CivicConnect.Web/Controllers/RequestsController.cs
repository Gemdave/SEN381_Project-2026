using CivicConnect.Core.Models;
using CivicConnect.Core.Services;
using CivicConnect.Web.Dtos;
using CivicConnect.Web.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace CivicConnect.Web.Controllers;

// Controllers only translate HTTP to a service call and back. The rules live in Core.
[Route("api/v1/requests")]
public class RequestsController(
    CurrentUserAccessor accessor,
    RequestService requests,
    AssignmentService assignments,
    StatusTransitionService transitions,
    NoteService notes) : ApiControllerBase(accessor)
{
    [HttpPost]
    public async Task<IActionResult> Submit(SubmitRequestBody body, CancellationToken ct)
    {
        var user = await UserAsync(ct);
        var input = new NewRequest(body.Title ?? "", body.Description ?? "", body.Location ?? "", body.CategoryId);
        var created = await requests.SubmitAsync(user, input, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, RequestView.From(created));
    }

    [HttpGet]
    public async Task<IActionResult> ListOwn(
        [FromQuery] RequestStatus? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var user = await UserAsync(ct);
        var result = await requests.ListOwnAsync(user, status, page, pageSize, ct);
        return Ok(ToPage(result));
    }

    [HttpGet("queue")]
    public async Task<IActionResult> Queue(
        [FromQuery] RequestStatus? status, [FromQuery] short? categoryId, [FromQuery] string? sort,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var user = await UserAsync(ct);
        var result = await requests.QueueAsync(user, status, categoryId, sort, page, pageSize, ct);
        return Ok(ToPage(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var user = await UserAsync(ct);
        var detail = await requests.GetAsync(user, id, ct);
        return Ok(new RequestDetailView(RequestView.From(detail.Request), detail.History, detail.Notes));
    }

    [HttpPost("{id:guid}/assignment")]
    public async Task<IActionResult> Accept(Guid id, CancellationToken ct)
    {
        var user = await UserAsync(ct);
        var updated = await assignments.AcceptAsync(user, id, ct);
        return Ok(RequestView.From(updated));
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> ChangeStatus(Guid id, StatusChangeBody body, CancellationToken ct)
    {
        var user = await UserAsync(ct);
        var updated = await transitions.TransitionAsync(user, id, body.Status, body.Reason, ct);
        return Ok(RequestView.From(updated));
    }

    [HttpPost("{id:guid}/notes")]
    public async Task<IActionResult> AddNote(Guid id, NoteBody body, CancellationToken ct)
    {
        var user = await UserAsync(ct);
        var note = await notes.AddAsync(user, id, body.Body, ct);
        return Created($"/api/v1/requests/{id}", note);
    }

    private static PageView<RequestView> ToPage(PagedResult<ServiceRequest> page) =>
        new(page.Items.Select(RequestView.From).ToList(), page.Page, page.PageSize, page.Total);
}
