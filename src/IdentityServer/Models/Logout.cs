namespace IdentityServer.Models;
using System.Security.Claims;
using System.Threading;

public class Logout
{
    public string? LogoutId { get; set; }
    public ClaimsPrincipal User { get; set; } = default!;
    public CancellationToken Ct { get; set; }
}