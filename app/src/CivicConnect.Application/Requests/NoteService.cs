using CivicConnect.Application.Abstractions;
using CivicConnect.Domain.Requests;

namespace CivicConnect.Application.Requests;

public enum NoteOutcome
{
    Added,
    Empty,
    TooLong,
    Forbidden
}

/// <summary>FR-013: record a comment on a request. Notes are history entries, so they cannot be edited or deleted.</summary>
public sealed class NoteService(
    IRequestHistoryRepository history,
    IAuthorisationPolicy policy,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    public const int MaxLength = 2000;

    public async Task<NoteOutcome> AddAsync(Guid requestId, Guid staffUserId, string? text, CancellationToken ct)
    {
        if (!await policy.MayAsync(staffUserId, RequestAction.AddNote, requestId, ct))
        {
            return NoteOutcome.Forbidden;
        }

        var clean = text?.Trim();
        if (string.IsNullOrEmpty(clean))
        {
            return NoteOutcome.Empty;
        }

        if (clean.Length > MaxLength)
        {
            return NoteOutcome.TooLong;
        }

        history.Add(RequestHistoryEntry.ForNote(requestId, staffUserId, clean, clock.UtcNow));
        await unitOfWork.SaveChangesAsync(ct);
        return NoteOutcome.Added;
    }
}
