namespace CivicConnect.Application.Results;

public enum SubmissionOutcome
{
    Created,
    Invalid,
    Forbidden
}

/// <summary>
/// AC-001.2: an invalid submission carries one message per failing field and
/// is never stored. Errors are keyed by the ADR-006 field names.
/// </summary>
public sealed record SubmissionResult(
    SubmissionOutcome Outcome, Guid? RequestId, string? Reference, IReadOnlyDictionary<string, string> Errors)
{
    private static readonly IReadOnlyDictionary<string, string> NoErrors = new Dictionary<string, string>();

    public static SubmissionResult Created(Guid requestId, string reference)
        => new(SubmissionOutcome.Created, requestId, reference, NoErrors);

    public static SubmissionResult Invalid(IReadOnlyDictionary<string, string> errors)
        => new(SubmissionOutcome.Invalid, null, null, errors);

    public static SubmissionResult Forbidden() => new(SubmissionOutcome.Forbidden, null, null, NoErrors);
}
