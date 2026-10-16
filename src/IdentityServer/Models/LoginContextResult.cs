namespace IdentityServer.Models;

public record LoginContextResult(string ClientId, string? ClientName, string? LoginHint);
