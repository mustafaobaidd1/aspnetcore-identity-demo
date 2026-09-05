using Microsoft.AspNetCore.Identity;

// Custom user: inherit IdentityUser and add whatever the app needs.
// EF Core maps new properties by convention — but the schema change
// still requires a migration.
public class AppUser : IdentityUser
{
    public string? FullName { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
