namespace IdentityServer.Dtos;

public record LogoutResponse(string? PostLogoutRedirectUri, string? SignOutIframeUrl, string? ClientName);