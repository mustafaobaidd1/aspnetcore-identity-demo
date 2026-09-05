# ASP.NET Core Identity — Web API demo

A minimal ASP.NET Core Web API that shows how to add login to a backend using
**ASP.NET Core Identity API endpoints** (`MapIdentityApi<TUser>`), backed by
EF Core and SQLite.

Built on **.NET 10**.

## What it demonstrates

- Identity API endpoints (`/register`, `/login`, `/refresh`, `/manage/*`) over a
  **custom user type** (`AppUser : IdentityUser`) with extra columns.
- Both authentication styles the endpoints support: **cookies**
  (`/login?useCookies=true`) and **bearer tokens** (`useCookies=false`).
- **Role-based authorization** with a policy (`AdminOnly`), plus roles and an
  admin account seeded at startup.
- **EF Core migrations** against SQLite, so accounts survive a restart.
- An interactive API UI (**Scalar**), enabled in Development only.

## Running it

```bash
dotnet restore
dotnet run
```

Then open `http://localhost:5199/scalar` for the interactive API reference, or
`http://localhost:5199/openapi/v1.json` for the raw OpenAPI document.

### Seeding an admin account

Admin credentials are read from configuration and are deliberately **not** in
source control. Without them the app still runs; it just creates no admin user.

Set them with user secrets:

```bash
dotnet user-secrets init
dotnet user-secrets set "Seed:AdminEmail" "admin@example.com"
dotnet user-secrets set "Seed:AdminPassword" "<a strong password>"
```

Or as environment variables:

```bash
export Seed__AdminEmail="admin@example.com"
export Seed__AdminPassword="<a strong password>"
```

The default Identity password rules require at least 6 characters with an
uppercase letter, a lowercase letter, a digit, and a non-alphanumeric character.

## Endpoints

| Endpoint | Auth | Notes |
| --- | --- | --- |
| `POST /register` | anonymous | Email and password only — see the note below |
| `POST /login` | anonymous | `?useCookies=true` for cookies, otherwise tokens |
| `POST /refresh` | anonymous | Exchanges a refresh token for a new access token |
| `POST /logout` | authenticated | Send `{}` as the body |
| `GET /me` | authenticated | Name, email and roles read from the claims |
| `PATCH /me/profile` | authenticated | Updates the custom `FullName` column |
| `GET /weatherforecast` | authenticated | Any signed-in user |
| `GET /admin/users` | `Admin` role | Returns 403, not 401, for a signed-in non-admin |

`MapIdentityApi` also maps `/confirmEmail`, `/resendConfirmationEmail`,
`/forgotPassword`, `/resetPassword`, `/manage/2fa` and `/manage/info`.

## Things worth knowing

**The access token is not a JWT.** `MapIdentityApi` issues a token proprietary
to ASP.NET Core Identity (it starts with `CfDJ8…`, a Data Protection payload).
Nothing outside this app can validate it. The ASP.NET Core documentation is
explicit that this option targets simple scenarios and is not a replacement for
an identity server. For standard JWTs, use Microsoft Entra ID or Duende
IdentityServer instead.

**`/register` accepts only email and password.** Custom profile fields need an
endpoint of their own — here, `PATCH /me/profile`.

**Rebuild after adding a migration.** `dotnet ef migrations add` builds the
project *before* it writes the migration files, so those files are not yet in
the compiled assembly. Running with `--no-build` right afterwards fails with
`No migrations were found in assembly` and then `no such table: AspNetRoles`.
Run `dotnet build` again first.

**Middleware order matters.** `UseAuthentication` must come before
`UseAuthorization`.

**The OpenAPI UI is Development-only.** Exposing Scalar, Swagger UI or ReDoc in
production discloses the full shape of the API.

## Not production-ready

This is a learning project. Before anything real:

- Move from SQLite to a server database and run migrations from the deployment
  pipeline rather than at application startup.
- Wire up a real `IEmailSender` and require confirmed accounts.
- Reconsider whether Identity's own tokens fit, or whether the app needs a
  proper identity provider.

## References

- [Introduction to Identity on ASP.NET Core](https://learn.microsoft.com/aspnet/core/security/authentication/identity?view=aspnetcore-10.0)
- [How to use Identity to secure a Web API backend for SPAs](https://learn.microsoft.com/aspnet/core/security/authentication/identity-api-authorization?view=aspnetcore-10.0)
- [Identity model customization in ASP.NET Core](https://learn.microsoft.com/aspnet/core/security/authentication/customize-identity-model?view=aspnetcore-10.0)
- [Use the generated OpenAPI documents](https://learn.microsoft.com/aspnet/core/fundamentals/openapi/using-openapi-documents?view=aspnetcore-10.0)
