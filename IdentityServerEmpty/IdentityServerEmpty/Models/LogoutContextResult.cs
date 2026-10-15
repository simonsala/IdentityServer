namespace IdentityServerEmpty.Models;

public record LogoutContextResult(bool ShowSignoutPrompt, string? ClientName);
