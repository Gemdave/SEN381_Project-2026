namespace CivicConnect.Web.Security;

public interface ICurrentUser
{
    Guid Id { get; }
}
