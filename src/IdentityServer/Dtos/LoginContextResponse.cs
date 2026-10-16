namespace IdentityServer.Dtos;

public record LoginContextResponse(string ClientId, string? ClientName, string? LoginHint);