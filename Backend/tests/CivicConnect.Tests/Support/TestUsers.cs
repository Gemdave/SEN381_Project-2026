using CivicConnect.Core.Models;

namespace CivicConnect.Tests.Support;

public static class TestUsers
{
    public static readonly string[] RequesterPermissions = [Permissions.Submit, Permissions.ViewOwn];

    public static readonly string[] StaffPermissions =
    [
        Permissions.Queue, Permissions.ViewDetail, Permissions.Assign,
        Permissions.UpdateStatus, Permissions.Close, Permissions.AddNote
    ];

    public static CurrentUser Requester(string name = "Rita") =>
        new(Guid.NewGuid(), name, RequesterPermissions.ToHashSet());

    public static CurrentUser Staff(string name = "Sam") =>
        new(Guid.NewGuid(), name, StaffPermissions.ToHashSet());

    public static CurrentUser StaffWithout(string permission, string name = "Sipho") =>
        new(Guid.NewGuid(), name, StaffPermissions.Where(p => p != permission).ToHashSet());

    public static NewRequest ValidRequest(short categoryId = 1) =>
        new("Broken light", "The light in corridor B flickers.", "Block B", categoryId);
}
