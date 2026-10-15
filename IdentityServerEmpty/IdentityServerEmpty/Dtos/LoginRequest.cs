namespace IdentityServerEmpty.Dtos;

public record LoginRequest(string Username, string Password, string ReturnUrl, bool RememberLogin = false);
