namespace IdentityServer.Models;
using System.ComponentModel.DataAnnotations;
public class Login
{
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string ReturnUrl { get; set; } = "";
    public bool RememberLogin { get; set; }
    public CancellationToken CancellationToken { get; set; }
}