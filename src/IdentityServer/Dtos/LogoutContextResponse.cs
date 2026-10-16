namespace IdentityServer.Dtos;

public record LogoutContextResponse(bool ShowSignoutPrompt, string? ClientName);