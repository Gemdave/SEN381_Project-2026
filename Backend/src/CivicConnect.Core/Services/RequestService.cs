using CivicConnect.Core.Abstractions;
using CivicConnect.Core.Models;
using CivicConnect.Core.Rules;

namespace CivicConnect.Core.Services;

public class RequestService(IUnitOfWorkFactory factory, AuthorisationPolicy policy)
{
    private static readonly string[] SortOptions = ["created", "-created", "status", "category"];

    // FR-001, FR-002
    public async Task<ServiceRequest> SubmitAsync(CurrentUser user, NewRequest input, CancellationToken ct = default)
    {
        policy.Require(user, Permissions.Submit);

        var errors = new Dictionary<string, string[]>();
        Validation.CheckText(errors, "title", input.Title, 120, "Give the request a short title.");
        Validation.CheckText(errors, "description", input.Description, 2000, "Describe what needs attention.");
        Validation.CheckText(errors, "location", input.Location, 200, "Say where this is.");
        if (input.CategoryId <= 0)
            errors["categoryId"] = ["Pick a category from the list."];
        if (errors.Count > 0)
            throw new ValidationFailedException(errors);

        await using var uow = await factory.BeginAsync(ct);

        var category = await uow.Categories.FindAsync(input.CategoryId, ct);
        if (category is null || !category.IsActive)
            throw new ValidationFailedException(new Dictionary<string, string[]>
            {
                ["categoryId"] = ["Pick a category from the list."]
            });

        var clean = input with
        {
            Title = input.Title.Trim(),
            Description = input.Description.Trim(),
            Location = input.Location.Trim()
        };

        var created = await uow.Requests.InsertAsync(user.Id, clean, ct);
        await uow.History.AddAsync(created.Id, "Submitted", null, RequestStatus.Received, user.Id, null, ct);
        await uow.CommitAsync(ct);
        return created;
    }

    // FR-003, FR-010. A request you may not see looks exactly like one that does not exist.
    public async Task<RequestDetail> GetAsync(CurrentUser user, Guid id, CancellationToken ct = default)
    {
        await using var uow = await factory.BeginAsync(ct);

        var request = await uow.Requests.FindAsync(id, ct);
        if (request is null || !policy.CanSee(user, request))
            throw new NotFoundException("We could not find that request.");

        if (!policy.CanSeeInternals(user))
            return new RequestDetail(request, null, null);

        var history = await uow.History.ListAsync(id, ct);
        var notes = await uow.Notes.ListAsync(id, ct);
        return new RequestDetail(request, history, notes);
    }

    // FR-004, FR-005
    public async Task<PagedResult<ServiceRequest>> ListOwnAsync(
        CurrentUser user, RequestStatus? status, int page, int pageSize, CancellationToken ct = default)
    {
        policy.Require(user, Permissions.ViewOwn);
        (page, pageSize) = Validation.Paging(page, pageSize);

        await using var uow = await factory.BeginAsync(ct);
        return await uow.Requests.ListForRequesterAsync(user.Id, status, page, pageSize, ct);
    }

    // FR-008, FR-009
    public async Task<PagedResult<ServiceRequest>> QueueAsync(
        CurrentUser user, RequestStatus? status, short? categoryId, string? sort,
        int page, int pageSize, CancellationToken ct = default)
    {
        policy.Require(user, Permissions.Queue);

        sort = string.IsNullOrWhiteSpace(sort) ? "created" : sort;
        if (!SortOptions.Contains(sort))
            throw new ValidationFailedException(new Dictionary<string, string[]>
            {
                ["sort"] = [$"Sort must be one of: {string.Join(", ", SortOptions)}."]
            });

        (page, pageSize) = Validation.Paging(page, pageSize);

        await using var uow = await factory.BeginAsync(ct);
        return await uow.Requests.ListQueueAsync(user.Id, new QueueFilter(status, categoryId, sort, page, pageSize), ct);
    }
}
