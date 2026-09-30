namespace CivicConnect.Core.Services;

internal static class Validation
{
    public static void CheckText(Dictionary<string, string[]> errors, string field, string? value,
        int max, string emptyMessage)
    {
        var text = value?.Trim() ?? "";
        if (text.Length == 0)
            errors[field] = [emptyMessage];
        else if (text.Length > max)
            errors[field] = [$"Please keep this to {max} characters or fewer."];
    }

    public static (int page, int pageSize) Paging(int page, int pageSize)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 50) pageSize = 50;
        return (page, pageSize);
    }
}
