namespace IdentityServer.Models;

public static class AccountErrors
{
    public static readonly Error InvalidReturnUrl = new("Account.InvalidReturnUrl", "The return URL is invalid.");
    public static readonly Error InvalidCredentials = new("Account.InvalidCredentials", "Invalid username or password.");
    public static readonly Error LockedOut = new("Account.LockedOut", "The account is locked out. Try again later.");
    public static readonly Error NotAllowed = new("Account.NotAllowed", "The account is not allowed to sign in.");
    public static readonly Error ErrorNotFound = new("Account.ErrorNotFound", "The error could not be found.");
    public static readonly Error InvalidSignUpRequest = new("Account.InvalidSignUpRequest", "The sign-up request is invalid.");
    public static readonly Error SignUpError = new("Account.SignUpError", "An error occurred during sign-up.");
}
