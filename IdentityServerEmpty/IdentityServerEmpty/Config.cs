using Duende.IdentityModel;
using Duende.IdentityServer;
using Duende.IdentityServer.Models;

namespace IdentityServerEmpty;

public static class Config
{
    public static IEnumerable<IdentityResource> IdentityResources =>
        new IdentityResource[]
        {
            new IdentityResources.OpenId(),
            new IdentityResources.Profile(),
            new IdentityResource("customer", new[] { "customer_number" }),
            new IdentityResource("roles", "User roles", new[] { JwtClaimTypes.Role })
        };

    public static IEnumerable<ApiScope> ApiScopes =>
        new ApiScope[]
            {
                new ApiScope(name: "ecommerceAPI", displayName: "Ecommerce API", userClaims: new[] { JwtClaimTypes.Role })
            };

    public static IEnumerable<Client> Clients =>
        new Client[]
            {
                new Client
                {
                    ClientId = "client",

                    // no interactive user, use the clientid/secret for authentication
                    AllowedGrantTypes = GrantTypes.ClientCredentials,

                    // secret for authentication
                    ClientSecrets =
                    {
                        new Secret("secret".Sha256())
                    },

                    // scopes that client has access to
                    AllowedScopes = { "ecommerceAPI" }
                },
                // interactive SPA
                new Client
                {
                    ClientId = "spa",
                    ClientSecrets = { new Secret("spa-secret".Sha256()) },

                    AllowedGrantTypes = GrantTypes.Code,
                    RequirePkce = true,

                    // where to redirect to after login
                    RedirectUris = { "https://localhost:5173/auth/callback" },

                    // where to redirect to after logout
                    PostLogoutRedirectUris = { "https://localhost:5173/" },

                    AllowedScopes =
                    {
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Profile,
                        "ecommerceAPI",
                        "customer",
                        "roles"
                    }
                }
            };
}
