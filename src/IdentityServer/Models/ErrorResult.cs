namespace IdentityServer.Models;

public record ErrorResult(string? Error, string? ErrorDescription, string? RequestId);
