namespace IdentityServerEmpty.Models;

public record LogoutResult(string? PostLogoutRedirectUri, string? SignOutIframeUrl, string? ClientName);
