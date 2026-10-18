namespace CivicConnect.Application.Abstractions;
/*CR-003. Turns a password into a salted, slow hash and checks a password against one.
The application never stores or logs a password, only what Hash returns.*/
public interface IPasswordHasher
{
    string Hash(string password);
    /*True when the password matches. A null or unreadable hash returns false, and still does the same work as a real check, 
    so a wrong email takes as long as a wrong password and cannot be told apart by timing.*/
    bool Verify(string? password, string? hash);
}
