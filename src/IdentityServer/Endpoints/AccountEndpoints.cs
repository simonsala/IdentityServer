using System.Security.Claims;
using IdentityServer.Dtos;
using IdentityServer.Models;
using IdentityServer.Services;
using Microsoft.AspNetCore.Http.HttpResults;

namespace IdentityServer.Endpoints;

public static class AccountEndpoints
{
    public const string CorsPolicyName = "spa";

    public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/account")
            .WithTags("Account")
            .RequireCors(CorsPolicyName);

        //login
        group.MapGet("/login-context", GetLoginContext);
        group.MapPost("/login", Login);
        group.MapGet("/logout-context", GetLogoutContext);
        group.MapPost("/logout", Logout);
        group.MapGet("/error", GetError);

        //signup
        group.MapPost("/signup", SignUp);

        return app;
    }

    private static async Task<Results<Ok<LoginContextResponse>, ProblemHttpResult>> GetLoginContext(
        string returnUrl, IAccountService accountService, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(returnUrl))
        {
            return ToProblem(AccountErrors.InvalidReturnUrl, StatusCodes.Status400BadRequest);
        }

        var result = await accountService.GetLoginContextAsync(new LoginContext { ReturnUrl = returnUrl, CancellationToken = ct });
        if (result.IsFailure)
        {
            return ToProblem(result.Error, StatusCodes.Status400BadRequest);
        }

        var response = new LoginContextResponse(result?.Value?.ClientId, result.Value.ClientName, result.Value.LoginHint);

        return TypedResults.Ok(response);
    }

    private static async Task<Results<Ok<LoginResponse>, ValidationProblem, ProblemHttpResult>> Login(
        LoginRequest request, IAccountService accountService, CancellationToken ct)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(request.Username))
        {
            errors[nameof(request.Username)] = ["Username is required."];
        }
        if (string.IsNullOrWhiteSpace(request.Password))
        {
            errors[nameof(request.Password)] = ["Password is required."];
        }
        if (string.IsNullOrWhiteSpace(request.ReturnUrl))
        {
            errors[nameof(request.ReturnUrl)] = ["ReturnUrl is required."];
        }
        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        var result = await accountService.LoginAsync(new Login
        {
            Username = request.Username,
            Password = request.Password,
            ReturnUrl = request.ReturnUrl,
            RememberLogin = request.RememberLogin,
            CancellationToken = ct
        });

        if (result.IsFailure)
        {
            var statusCode = result.Error == AccountErrors.InvalidReturnUrl
                ? StatusCodes.Status400BadRequest
                : StatusCodes.Status401Unauthorized;
            return ToProblem(result.Error, statusCode);
        }

        return TypedResults.Ok(new LoginResponse { RedirectUrl = result.Value.RedirectUrl });
    }

    private static async Task<Ok<LogoutContextResponse>> GetLogoutContext(
        string? logoutId, ClaimsPrincipal user, IAccountService accountService, CancellationToken ct)
    {
        var result = await accountService.GetLogoutContextAsync(new LogoutContext { LogoutId = logoutId, User = user, Ct = ct });
        var response = new LogoutContextResponse(result.Value.ShowSignoutPrompt, result.Value.ClientName);
        return TypedResults.Ok(response);
    }

    private static async Task<Ok<LogoutResponse>> Logout(
        LogoutRequest request, ClaimsPrincipal user, IAccountService accountService, CancellationToken ct)
    {
        var result = await accountService.LogoutAsync(new Logout { LogoutId = request.LogoutId, User = user, Ct = ct });
        var response = new LogoutResponse(result.Value.PostLogoutRedirectUri, result.Value.SignOutIframeUrl, result.Value.ClientName);
        return TypedResults.Ok(response);
    }

    private static async Task<Results<Ok<SignUpResult>, ValidationProblem, ProblemHttpResult>> SignUp(
        SignUpRequest request, IAccountService accountService, CancellationToken ct)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(request.FirstName))
        {
            errors[nameof(request.FirstName)] = ["FirstName is required."];
        }
        if (string.IsNullOrWhiteSpace(request.LastName))
        {
            errors[nameof(request.LastName)] = ["LastName is required."];
        }
        if (string.IsNullOrWhiteSpace(request.Username))
        {
            errors[nameof(request.Username)] = ["Username is required."];
        }
        if (string.IsNullOrWhiteSpace(request.Password))
        {
            errors[nameof(request.Password)] = ["Password is required."];
        }
        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }
        
        var result = await accountService.SignUpAsync(new SignUp
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Username = request.Username,
            Password = request.Password
        });

        if (result.IsFailure)
        {
            var statusCode = result.Error == AccountErrors.SignUpError ? 
                                             StatusCodes.Status500InternalServerError :
                                             StatusCodes.Status400BadRequest;

            return ToProblem(result.Error, statusCode);
        }

        return TypedResults.Ok(result.Value);
    }

    private static async Task<Results<Ok<ErrorResult>, ProblemHttpResult>> GetError(
        string errorId, IAccountService accountService, CancellationToken ct)
    {
        var result = await accountService.GetErrorAsync(errorId, ct);
        if (result.IsFailure)
        {
            return ToProblem(result.Error, StatusCodes.Status404NotFound);
        }

        return TypedResults.Ok(result.Value);
    }

    private static ProblemHttpResult ToProblem(Error error, int statusCode) =>
        TypedResults.Problem(
            title: error.Message,
            statusCode: statusCode,
            extensions: new Dictionary<string, object?> { ["code"] = error.Code });
}