using CivicConnect.Domain.Requests;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace CivicConnect.Web.TagHelpers;

/// <summary>
/// AC-003.1. Status is rendered in exactly one place, so the words a requester
/// reads cannot drift from the words staff set.
/// </summary>
[HtmlTargetElement("status-badge")]
public sealed class StatusBadgeTagHelper : TagHelper
{
    public RequestStatus Status { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "span";
        output.Attributes.SetAttribute("class", "status status-" + Status.ToString().ToLowerInvariant());
        output.Content.SetContent(Plain(Status));
    }

    private static string Plain(RequestStatus status) => status switch
    {
        RequestStatus.Received => "Received",
        RequestStatus.Assigned => "Assigned",
        RequestStatus.InProgress => "In progress",
        RequestStatus.Resolved => "Resolved",
        RequestStatus.Closed => "Closed",
        _ => "Unknown"
    };
}
