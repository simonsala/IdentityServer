using System.Globalization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Serilog;
using Serilog.Filters;
using Microsoft.EntityFrameworkCore;
using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Mappers;
using IdentityServer.Data;
using IdentityServer.Endpoints;
using IdentityServer.Services;

namespace IdentityServer;

internal static class HostingExtensions
{
    public static WebApplicationBuilder ConfigureLogging(this WebApplicationBuilder builder)
    {
        // Set up logging to write regular entries to console, and diagnostics data to a file.
        // See https://duende.link/diagnostics
        _ = builder.Services.AddSerilog(lc =>
        {
            _ = lc.WriteTo.Logger(consoleLogger =>
            {
                _ = consoleLogger.WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}{NewLine}",
                    formatProvider: CultureInfo.InvariantCulture);
                if (builder.Environment.IsDevelopment())
                {
                    _ = consoleLogger.Filter.ByExcluding(Matching.FromSource("Duende.IdentityServer.Diagnostics.Summary"));
                }
            });
            if (builder.Environment.IsDevelopment())
            {
                _ = lc.WriteTo.Logger(fileLogger =>
                {
                    _ = fileLogger
                        .WriteTo.File("./diagnostics/diagnostic.log", rollingInterval: RollingInterval.Day,
                            fileSizeLimitBytes: 1024 * 1024 * 10, // 10 MB
                            rollOnFileSizeLimit: true,
                            outputTemplate: "[{Timestamp:HH:mm:ss} {Level}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}{NewLine}",
                            formatProvider: CultureInfo.InvariantCulture)
                        .Filter
                        .ByIncludingOnly(Matching.FromSource("Duende.IdentityServer.Diagnostics.Summary"));
                }).Enrich.FromLogContext().ReadFrom.Configuration(builder.Configuration);
            }
        });
        return builder;
    }

    public static WebApplication ConfigureServices(this WebApplicationBuilder builder)
    {
        // uncomment if you want to add a UI
        //builder.Services.AddRazorPages();
        const string connectionString = @"Server=(localdb)\MSSQLLocalDB;Database=IdentityServer;Trusted_Connection=True;TrustServerCertificate=True;";
        var migrationsAssembly = typeof(Program).Assembly.GetName().Name;

        builder.Services.AddDbContext<ApplicationDbContext>(o =>
            o.UseSqlServer(connectionString, sql => sql.MigrationsAssembly(migrationsAssembly)));

        builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        const string spaUrl = "https://localhost:5173";

        builder.Services.AddIdentityServer(options =>
        {
            // user interaction pages are hosted by the SPA instead of Razor Pages
            options.UserInteraction.LoginUrl = $"{spaUrl}/login";
            options.UserInteraction.LoginReturnUrlParameter = "returnUrl";
            options.UserInteraction.LogoutUrl = $"{spaUrl}/logout";
            options.UserInteraction.LogoutIdParameter = "logoutId";
            options.UserInteraction.ErrorUrl = $"{spaUrl}/error";
            options.UserInteraction.ErrorIdParameter = "errorId";
        })
        .AddConfigurationStore(options =>
        {
            options.ConfigureDbContext = b => b.UseSqlServer(connectionString,
                sql => sql.MigrationsAssembly(migrationsAssembly));
        })
        .AddOperationalStore(options =>
        {
            options.ConfigureDbContext = b => b.UseSqlServer(connectionString,
                sql => sql.MigrationsAssembly(migrationsAssembly));
        })
        .AddAspNetIdentity<ApplicationUser>()
        .AddProfileService<CustomerProfileService>();

        builder.Services.AddScoped<IAccountService, AccountService>();
        // add `.PersistKeysTo…()` and `.ProtectKeysWith…()` calls
        // see more at https://docs.duendesoftware.com/general/data-protection
        _ = builder.Services.AddDataProtection()
                   .SetApplicationName("IdentityServer");

        builder.Services.AddHttpContextAccessor();

        return builder.Build();
    }

    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        _ = app.UseSerilogRequestLogging();

        if (app.Environment.IsDevelopment())
        {
            _ = app.UseDeveloperExceptionPage();
        }

        InitializeDatabase(app);

        // uncomment if you want to add a UI
        //app.UseStaticFiles();
        //app.UseRouting();

        _ = app.UseIdentityServer();

        _ = app.MapAccountEndpoints();

        // uncomment if you want to add a UI
        //app.UseAuthorization();
        //app.MapRazorPages().RequireAuthorization();

        return app;
    }

    private static void InitializeDatabase(IApplicationBuilder app)
    {
        using (var serviceScope = app.ApplicationServices.GetService<IServiceScopeFactory>()!.CreateScope())
        {
            serviceScope.ServiceProvider.GetRequiredService<PersistedGrantDbContext>().Database.Migrate();
            serviceScope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.Migrate();

            var context = serviceScope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();
            context.Database.Migrate();

            // Add anything in Config that is missing from the database so config changes reach existing databases.
            var clients = context.Clients.Include(c => c.AllowedScopes).ToList();
            foreach (var client in Config.Clients)
            {
                var entity = clients.FirstOrDefault(c => c.ClientId == client.ClientId);
                if (entity is null)
                {
                    context.Clients.Add(client.ToEntity());
                    continue;
                }

                foreach (var scope in client.AllowedScopes.Except(entity.AllowedScopes.Select(s => s.Scope)))
                {
                    entity.AllowedScopes.Add(new ClientScope { Scope = scope });
                }
            }

            var identityResources = context.IdentityResources.Select(r => r.Name).ToList();
            foreach (var resource in Config.IdentityResources.Where(r => !identityResources.Contains(r.Name)))
            {
                context.IdentityResources.Add(resource.ToEntity());
            }

            var apiScopes = context.ApiScopes.Include(s => s.UserClaims).ToList();
            foreach (var scope in Config.ApiScopes)
            {
                var entity = apiScopes.FirstOrDefault(s => s.Name == scope.Name);
                if (entity is null)
                {
                    context.ApiScopes.Add(scope.ToEntity());
                    continue;
                }

                foreach (var claim in scope.UserClaims.Except(entity.UserClaims.Select(c => c.Type)))
                {
                    entity.UserClaims.Add(new ApiScopeClaim { Type = claim });
                }
            }

            context.SaveChanges();
        }
    }
}
