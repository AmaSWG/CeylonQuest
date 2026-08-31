# Identity Service

ASP.NET Core (net8.0) service owning users, authentication, provider applications and
the admin dashboard APIs.

## Configuration

`appsettings.json` holds only non-secret defaults. Secrets and environment-specific
values come from environment variables (or user-secrets locally). ASP.NET Core maps
`__` in an environment variable name to `:` in configuration, so `Email__Password`
sets `Email:Password`.

| Setting | Env var | Required | Notes |
|---|---|---|---|
| `ConnectionStrings:IdentityDb` | `ConnectionStrings__IdentityDb` | Production | Without it the service uses an in-memory database outside Production, and refuses to start in Production. |
| `Email:Password` | `Email__Password` | Production | Gmail app password. Password-reset emails fail without it. |
| `Email:ResetPasswordBaseUrl` | `Email__ResetPasswordBaseUrl` | Production | Base URL of the SPA reset page. Set per environment — a wrong value emails users a link they cannot open. |
| `Jwt:Key` | `Jwt__Key` | Production | Falls back to a well-known development key if unset. **Set this in Production.** |
| `Cors:AllowedOrigins` | `Cors__AllowedOrigins__0` | No | Array of allowed browser origins. Defaults to localhost dev ports. |
| `DatabaseServerVersion` | `DatabaseServerVersion` | No | MySQL version string, defaults to `8.0.29`. |

Production values that are *not* secret (CORS origins, reset URL) live in
`appsettings.Production.json` and are committed.

The service logs an error at startup in Production if `Email:Password` or `Jwt:Key`
is missing, rather than failing silently at first use.

### Local development

```bash
# from the repo root
dotnet user-secrets --project services/identity-service set "Email:Password" "<app password>"
dotnet run --project services/identity-service --launch-profile http
```

Runs on `http://localhost:5278`. With no connection string configured it uses an
in-memory database seeded with a default admin (see `Data/DbSeeder.cs`), so no MySQL
instance is needed to work on the API.

Use the **`http`** launch profile. HTTPS redirection is disabled in Development
because the Vite dev proxy speaks plain HTTP and cannot follow a redirect to the
self-signed HTTPS port.

## Database schema

The schema is owned entirely by EF Core migrations in `Migrations/`. Add changes with:

```bash
cd services/identity-service
ConnectionStrings__IdentityDb="<any valid connection string>" dotnet ef migrations add <Name>
```

Migrations are applied automatically at startup unless `DisableMigrations=true`.

## Tests

```bash
dotnet test services/identity-service/identity-service.Tests/IdentityService.Tests.csproj
```
