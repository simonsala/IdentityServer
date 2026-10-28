using System.Security.Claims;
using Duende.IdentityModel;
using Duende.IdentityServer.AspNetIdentity;
using Duende.IdentityServer.Models;
using Microsoft.AspNetCore.Identity;

namespace IdentityServer.Data;

public class CustomerProfileService(
    UserManager<ApplicationUser> userManager,
    IUserClaimsPrincipalFactory<ApplicationUser> claimsFactory)
    : ProfileService<ApplicationUser>(userManager, claimsFactory)
{
    protected override async Task GetProfileDataAsync(ProfileDataRequestContext context, ApplicationUser user)
    {
        var principal = await GetUserClaimsAsync(user);
        var identity = (ClaimsIdentity)principal.Identity!;
        // use OIDC claim names; AddRequestedClaims drops anything not requested by the "profile" scope
        identity.AddClaim(new Claim(JwtClaimTypes.GivenName, user.FirstName));
        identity.AddClaim(new Claim(JwtClaimTypes.FamilyName, user.LastName));

        context.AddRequestedClaims(principal.Claims);
    }
}