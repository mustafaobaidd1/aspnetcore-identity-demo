using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

public static class SeedData
{
    public const string AdminRole = "Admin";
    public const string UserRole = "User";

    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;

        var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(SeedData));
        var config = sp.GetRequiredService<IConfiguration>();

        // Apply pending migrations at startup. Convenient for a demo; in
        // production run `dotnet ef database update` from the deployment
        // pipeline instead, so concurrent instances don't race each other.
        await sp.GetRequiredService<ApplicationDbContext>().Database.MigrateAsync();

        var roleManager = sp.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in new[] { AdminRole, UserRole })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        // Credentials come from configuration, never from source. Set them with
        // user secrets (see README) or environment variables. Without them the
        // app still runs — it just has no admin account.
        var adminEmail = config["Seed:AdminEmail"];
        var adminPassword = config["Seed:AdminPassword"];

        if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
        {
            logger.LogWarning(
                "Seed:AdminEmail / Seed:AdminPassword are not configured, so no admin user was created. " +
                "See the README for how to set them.");
            return;
        }

        var userManager = sp.GetRequiredService<UserManager<AppUser>>();
        if (await userManager.FindByEmailAsync(adminEmail) is not null) return;

        var admin = new AppUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true,
            FullName = "Site Administrator"
        };

        var result = await userManager.CreateAsync(admin, adminPassword);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(admin, AdminRole);
            logger.LogInformation("Seeded admin account {Email}.", adminEmail);
        }
        else
        {
            logger.LogError("Failed to seed the admin account: {Errors}",
                string.Join("; ", result.Errors.Select(e => e.Description)));
        }
    }
}
