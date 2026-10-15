namespace CivicConnect.Web.Models;

public enum MessageTone
{
    Success,
    Error
}

/// <summary>
/// The one-line result shown at the top of a page after an action. The tone
/// decides how it looks (green or red) and how a screen reader announces it,
/// so a refusal can never be mistaken for a success.
/// </summary>
public sealed record PageMessage(string Text, MessageTone Tone)
{
    public static PageMessage Success(string text) => new(text, MessageTone.Success);

    public static PageMessage Error(string text) => new(text, MessageTone.Error);

    public static PageMessage From(bool succeeded, string text) => succeeded ? Success(text) : Error(text);
}
