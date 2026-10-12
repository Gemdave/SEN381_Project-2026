using CivicConnect.Application.Abstractions;
using CivicConnect.Application.Results;
using CivicConnect.Domain.Requests;

namespace CivicConnect.Application.Requests;

public sealed record NewRequest(string? Title, string? Description, string? Location, Guid CategoryId);

/// <summary>
/// FR-001 and FR-002. Every failing field is reported at once with its own
/// message (AC-001.2, AC-002.2), so nothing incomplete is ever stored. The
/// request and its first history entry are written in one transaction.
/// </summary>
public sealed class SubmissionService(
    IRequestRepository requests,
    IRequestHistoryRepository history,
    IRequestQueries queries,
    IAuthorisationPolicy policy,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task<SubmissionResult> SubmitAsync(Guid requesterId, NewRequest input, CancellationToken ct)
    {
        if (!await policy.MaySubmitAsync(requesterId, ct))
        {
            return SubmissionResult.Forbidden();
        }

        var errors = new Dictionary<string, string>();
        var title = Check(errors, "title", input.Title, Request.TitleMax, "Give the request a short title.");
        var description = Check(errors, "description", input.Description, Request.DescriptionMax, "Describe what needs attention.");
        var location = Check(errors, "location", input.Location, Request.LocationMax, "Say where this is.");
        if (input.CategoryId == Guid.Empty || !await queries.IsActiveCategoryAsync(input.CategoryId, ct))
        {
            errors["categoryId"] = "Pick a category from the list.";
        }

        if (errors.Count > 0)
        {
            return SubmissionResult.Invalid(errors);
        }

        return await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var now = clock.UtcNow;
            var number = await requests.NextReferenceNumberAsync(token);
            var request = Request.Submit(
                reference: "REQ-" + number.ToString("D6"),
                title: title,
                description: description,
                location: location,
                categoryId: input.CategoryId,
                requesterId: requesterId,
                now: now);

            await requests.AddAsync(request, token);
            history.Add(RequestHistoryEntry.ForSubmission(request.Id, requesterId, now));
            await unitOfWork.SaveChangesAsync(token);

            return SubmissionResult.Created(request.Id, request.Reference);
        }, ct);
    }

    private static string Check(Dictionary<string, string> errors, string field, string? value, int max, string emptyMessage)
    {
        var text = value?.Trim() ?? string.Empty;
        if (text.Length == 0)
        {
            errors[field] = emptyMessage;
        }
        else if (text.Length > max)
        {
            errors[field] = $"Please keep this to {max} characters or fewer.";
        }

        return text;
    }
}
