using CivicConnect.Domain.Requests;

namespace CivicConnect.Web.Models;

/// <summary>
/// A request's history with the names of the people in it, so the page reads
/// "by Dev Staff One" rather than an id (OBS-02). Unknown ids fall back to the id.
/// </summary>
public sealed record HistoryView(IReadOnlyList<RequestHistoryEntry> Entries, IReadOnlyDictionary<Guid, string> Names)
{
    public string NameOf(Guid userId) => Names.TryGetValue(userId, out var name) ? name : userId.ToString();

    /// <summary>Assignment stores the new owner's id as its value; status changes store a status name.</summary>
    public string ValueText(string? value)
    {
        if (value is null)
        {
            return string.Empty;
        }

        if (Guid.TryParse(value, out var id))
        {
            return NameOf(id);
        }

        return Enum.TryParse<RequestStatus>(value, out var status) ? Plain(status) : value;
    }

    /// <summary>The ids an entry refers to, for the name lookup.</summary>
    public static IEnumerable<Guid> PeopleIn(IEnumerable<RequestHistoryEntry> entries)
        => entries.SelectMany(e => new[] { e.ActorUserId }
            .Concat(Guid.TryParse(e.NewValue, out var owner) ? [owner] : []));

    public static string ActionText(string action) => action switch
    {
        "StatusChanged" => "Status changed",
        "NoteAdded" => "Note added",
        _ => action
    };

    private static string Plain(RequestStatus status) => status switch
    {
        RequestStatus.InProgress => "In progress",
        _ => status.ToString()
    };
}
