namespace IdentityServer.Models;

public class SignUp
{
    public string FirstName { get; set; } = default!;
    
    public string LastName { get; set; } = default!;

    public string Username { get; set; } = default!;

    public string Password { get; set; } = default!;

    public string RedirectUrl { get; set; } = "";

    public CancellationToken CancellationToken { get; set; }
}