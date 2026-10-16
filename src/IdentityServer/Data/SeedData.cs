using Microsoft.AspNetCore.Identity;

namespace IdentityServer.Data;

public static class SeedData
{
    // Development-only credentials; never run this seeder in production.
    private const string TestPassword = "Pass123$";

    private static (ApplicationUser User, string[] Roles)[] TestUsers =>
    [
        (new ApplicationUser
        {
            UserName = "alice@example.com",
            Email = "alice@example.com",
            EmailConfirmed = true,
            FirstName = "Alice",
            LastName = "Smith",
        }, [AppRoles.Admin, AppRoles.Customer]),
        (new ApplicationUser
        {
            UserName = "bob@example.com",
            Email = "bob@example.com",
            EmailConfirmed = true,
            FirstName = "Bob",
            LastName = "Smith"
        }, [AppRoles.Customer])
    ];

    public static async Task EnsureSeedDataAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        foreach (var role in AppRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                EnsureSucceeded(await roleManager.CreateAsync(new IdentityRole(role)));
            }
        }

        foreach (var (seedUser, roles) in TestUsers)
        {
            var user = await userManager.FindByNameAsync(seedUser.UserName!);
            if (user is null)
            {
                user = seedUser;
                EnsureSucceeded(await userManager.CreateAsync(user, TestPassword));
            }

            var missingRoles = roles.Except(await userManager.GetRolesAsync(user)).ToArray();
            if (missingRoles.Length > 0)
            {
                EnsureSucceeded(await userManager.AddToRolesAsync(user, missingRoles));
            }
        }
    }

    private static void EnsureSucceeded(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));
        }
    }
}
