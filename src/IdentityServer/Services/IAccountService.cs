using IdentityServer.Models;

namespace IdentityServer.Services;

public interface IAccountService
{
    // Login
    Task<Result<LoginContextResult>> GetLoginContextAsync(LoginContext loginContext);

    Task<Result<LoginResult>> LoginAsync(Login login);

    Task<LogoutContextResult> GetLogoutContextAsync(LogoutContext logoutContext);

    Task<LogoutResult> LogoutAsync(Logout logout);

    Task<Result<ErrorResult>> GetErrorAsync(string errorId, CancellationToken ct);

    // Sign up
    Task<Result<SignUpResult>> SignUpAsync(SignUp signUp);
    // Task<Result<SignUpResult>> ConfirmSignUpAsync(ConfirmSignUpRequest confirmSignUpRequest);



}