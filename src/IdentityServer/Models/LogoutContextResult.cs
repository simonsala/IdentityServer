namespace IdentityServer.Models;

public record LogoutContextResult(bool ShowSignoutPrompt, string? ClientName);
