namespace IdentityServerEmpty.Models;
using System.Security.Claims;
using System.Threading;
public class LogoutContext
{
    public string? LogoutId { get; set; }
    public ClaimsPrincipal User { get; set; } = default!;
    public CancellationToken Ct { get; set; }
}