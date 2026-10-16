using Duende.IdentityServer;
using Duende.IdentityServer.Events;
using Duende.IdentityServer.Extensions;
using Duende.IdentityServer.Services;   
using IdentityServer.Data;
using IdentityServer.Models;
using Microsoft.AspNetCore.Authentication; 
using Microsoft.AspNetCore.Identity;

namespace IdentityServer.Services;

public class AccountService(
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager,
    IIdentityServerInteractionService interaction,
    IEventService events,
    IHostEnvironment environment,
    IHttpContextAccessor httpContextAccessor) : IAccountService
{
    public async Task<Result<LoginContextResult>> GetLoginContextAsync(LoginContext loginContext)
    {
        var context = await interaction.GetAuthorizationContextAsync(loginContext.ReturnUrl, loginContext.CancellationToken);
        if (context is null)
        {
            return AccountErrors.InvalidReturnUrl;
        }

        return new LoginContextResult(context.Client.ClientId, context.Client.ClientName, context.LoginHint);
    }

    public async Task<Result<LoginResult>> LoginAsync(Login login)
    {
        var returnUrl = login.ReturnUrl;
        var ct = login.CancellationToken;

        // only allow redirects back to a valid IdentityServer authorize request (prevents open redirects)
        var context = await interaction.GetAuthorizationContextAsync(returnUrl, ct);
        if (context is null)
        {
            return AccountErrors.InvalidReturnUrl;
        }

        var clientId = context.Client.ClientId;
        var username = login.Username;
        var password = login.Password;
        var rememberLogin = login.RememberLogin;

        var user = await userManager.FindByEmailAsync(username) ?? await userManager.FindByNameAsync(username);
        if (user is null)
        {
            await events.RaiseAsync(new UserLoginFailureEvent(username, "invalid credentials", clientId: clientId), ct);
            return AccountErrors.InvalidCredentials;
        }

        var result = await signInManager.PasswordSignInAsync(user, password, isPersistent: rememberLogin, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            await events.RaiseAsync(new UserLoginSuccessEvent(user.UserName, user.Id, user.UserName, clientId: clientId), ct);
            
            var props = new AuthenticationProperties();
            if (rememberLogin)
            {
                props.IsPersistent = true; 
            }

            var isuser = new IdentityServerUser(user.Id)
            {
                DisplayName = user.UserName
            };

            await httpContextAccessor.HttpContext.SignInAsync(isuser, props);
            return new LoginResult { RedirectUrl = returnUrl };
        }

        await events.RaiseAsync(new UserLoginFailureEvent(username, "invalid credentials", clientId: clientId), ct);

        if (result.IsLockedOut)
        {
            return AccountErrors.LockedOut;
        }

        if (result.IsNotAllowed)
        {
            return AccountErrors.NotAllowed;
        }

        return AccountErrors.InvalidCredentials;
    }

    public async Task<LogoutContextResult> GetLogoutContextAsync(LogoutContext logoutContext)
    {
        var logoutId = logoutContext.LogoutId;
        var user = logoutContext.User;
        var ct = logoutContext.Ct;

        var context = await interaction.GetLogoutContextAsync(logoutId, ct);
        var showSignoutPrompt = user.Identity?.IsAuthenticated == true && context.ShowSignoutPrompt;

        return new LogoutContextResult(showSignoutPrompt, context.ClientName);
    }

    public async Task<LogoutResult> LogoutAsync(Logout logout)
    {
        var logoutId = logout.LogoutId;
        var user = logout.User;
        var ct = logout.Ct;

        if (user.Identity?.IsAuthenticated == true)
        {
            // capture the current session before signing out so other clients can be notified
            logoutId ??= await interaction.CreateLogoutContextAsync(ct);
            await signInManager.SignOutAsync();
            if (httpContextAccessor?.HttpContext?.User?.Identity?.IsAuthenticated == true)
            {
                await httpContextAccessor.HttpContext.SignOutAsync();
            }
            await events.RaiseAsync(new UserLogoutSuccessEvent(user.GetSubjectId(), user.GetDisplayName()), ct);
        }

        var context = await interaction.GetLogoutContextAsync(logoutId, ct);

        return new LogoutResult(context.PostLogoutRedirectUri, context.SignOutIFrameUrl, context.ClientName);
    }

    public async Task<Result<SignUpResult>> SignUpAsync(SignUp signUp)
    {
        if (signUp is null)
        {
            return AccountErrors.InvalidSignUpRequest;
        }

        var user = new ApplicationUser
        {
            UserName = signUp.Email,
            Email = signUp.Email,
            FirstName = signUp.FirstName,
            LastName = signUp.LastName
        };
        var result = await userManager.CreateAsync(user, signUp.Password);

        if (!result.Succeeded)
        {
            return AccountErrors.InvalidSignUpRequest;
        }

        var roleResult = await userManager.AddToRoleAsync(user, AppRoles.Customer);
        if (!roleResult.Succeeded)
        {
            return AccountErrors.InvalidSignUpRequest;
        }

        return new SignUpResult { Success = true };
    }

    public async Task<Result<ErrorResult>> GetErrorAsync(string errorId, CancellationToken ct)
    {
        var message = await interaction.GetErrorContextAsync(errorId, ct);
        if (message is null)
        {
            return AccountErrors.ErrorNotFound;
        }

        // don't leak error details outside development
        var description = environment.IsDevelopment() ? message.ErrorDescription : null;

        return new ErrorResult(message.Error, description, message.RequestId);
    }
}