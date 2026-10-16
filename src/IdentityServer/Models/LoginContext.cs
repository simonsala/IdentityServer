namespace IdentityServer.Models;

public class LoginContext
{
    public string ReturnUrl { get; set; } = "";
    public CancellationToken CancellationToken { get; set; }
}