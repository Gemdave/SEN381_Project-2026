using CivicConnect.Core.Models;

namespace CivicConnect.Web.UI;

/// <summary>
/// Keeps the shared summary partial free of role knowledge: staff follow a
/// reference into the staff detail page, requesters into their own.
/// </summary>
public static class RequestPaths
{
    public static string DetailPath(this ServiceRequest request)
        => "/Requests/Details?id=" + request.Id;

    public static string StaffDetailPath(this ServiceRequest request)
        => "/Staff/Details?id=" + request.Id;
}
