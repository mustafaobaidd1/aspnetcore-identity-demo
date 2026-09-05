using Scalar.AspNetCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

// 1) EF Core over SQLite — data now survives a restart.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2) Authorization + a role-based policy.
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole(SeedData.AdminRole));
});

// 3) Identity API endpoints, with roles, over the custom AppUser.
builder.Services.AddIdentityApiEndpoints<AppUser>()
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

var app = builder.Build();

// 4) Migrate + seed roles and the admin account.
await SeedData.InitializeAsync(app.Services);

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

// 5) /register, /login, /refresh, /manage/*, ...
app.MapIdentityApi<AppUser>();

// 6) Logout — not provided by MapIdentityApi.
app.MapPost("/logout", async (SignInManager<AppUser> signInManager,
    [FromBody] object empty) =>
{
    if (empty != null)
    {
        await signInManager.SignOutAsync();
        return Results.Ok();
    }
    return Results.Unauthorized();
})
.RequireAuthorization();

// 7) Who am I — reads claims straight off the principal.
app.MapGet("/me", (ClaimsPrincipal user) => Results.Ok(new
{
    name = user.Identity?.Name,
    email = user.FindFirstValue(ClaimTypes.Email),
    roles = user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray()
}))
.RequireAuthorization();

// 8) MapIdentityApi's /register only accepts email + password, so custom
//    profile fields need an endpoint of their own.
app.MapPatch("/me/profile", async (UserManager<AppUser> userManager,
    ClaimsPrincipal principal, ProfileUpdate update) =>
{
    var user = await userManager.GetUserAsync(principal);
    if (user is null) return Results.Unauthorized();

    user.FullName = update.FullName;
    var result = await userManager.UpdateAsync(user);

    return result.Succeeded
        ? Results.Ok(new { user.Email, user.FullName, user.CreatedAt })
        : Results.ValidationProblem(result.Errors
            .ToDictionary(e => e.Code, e => new[] { e.Description }));
})
.RequireAuthorization();

// 9) Any authenticated user.
var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    return Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
})
.WithName("GetWeatherForecast")
.RequireAuthorization();

// 10) Admin role only.
app.MapGet("/admin/users", async (UserManager<AppUser> userManager) =>
    Results.Ok(await userManager.Users
        .Select(u => new { u.Email, u.FullName, u.CreatedAt })
        .ToListAsync()))
.RequireAuthorization("AdminOnly");

app.Run();

record ProfileUpdate(string? FullName);

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
