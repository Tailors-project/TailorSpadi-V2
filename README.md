# TailorSpadi

Module 1 foundation: one .NET 10 API, one PostgreSQL database, and three React
shells. Accounts, tenant resolution, and business features are intentionally
deferred. Placeholder routes do not grant access to any business data.

## Quick start: local development

Prerequisites: .NET SDK 10.0.201 or a later .NET 10 feature band, Node.js 22.12+
(use Node 22 LTS), and Docker Desktop with Linux containers for PostgreSQL.
You can also use a separately installed PostgreSQL 17 instance.

Run these commands from the repository root in PowerShell:

```powershell
./scripts/Initialize-Local.ps1
docker compose up -d postgres
dotnet restore TailorSpadi.sln --locked-mode
dotnet tool restore
npm ci
dotnet run --project src/Backend/TailorSpadi.Api
```

The initialization script generates a random local database password in the
ignored `.env` and saves the matching connection string in .NET user secrets.
It preserves an existing `.env`. Neither file should be committed.

Start the frontends in three additional terminals:

```powershell
npm run dev:storefront
npm run dev:backoffice
npm run dev:admin
```

| Component | Local URL |
| --- | --- |
| Storefront | http://localhost:5173 |
| Registration / onboarding | http://localhost:5174/register / http://localhost:5174/onboarding |
| Business login / workspace | http://localhost:5174/login / http://localhost:5174/backoffice |
| Administration | http://localhost:5175/admin |
| API metadata | http://localhost:5080/api/system-info |
| Swagger UI (Development only) | http://localhost:5080/swagger |
| Liveness / readiness | http://localhost:5080/health/live / http://localhost:5080/health/ready |
| PostgreSQL | localhost:54329 |

Vite forwards each shell's same-origin `/api` calls to port 5080.
The metadata call is demonstrated only in the storefront. API credentials,
connection strings, and access tokens must never go into frontend variables.

For a separately installed database, set its connection string instead:

```powershell
dotnet user-secrets set "ConnectionStrings:Postgres" "Host=localhost;Port=5432;Database=tailorspadi;Username=YOUR_USER;Password=YOUR_PASSWORD" --project src/Backend/TailorSpadi.Api
```

Create that database/user yourself with PostgreSQL administration tools.
Module 1 has no business schema and does not create or migrate databases.

## Docker: the complete application

Stop the local API first because both modes use port 5080.

```powershell
./scripts/Initialize-Local.ps1
docker compose up --build -d --wait
docker compose ps
./scripts/Verify-Routes.ps1
```

Open http://localhost:8080 for the storefront. Main-domain paths `/register`,
`/onboarding`, and `/admin` select their corresponding frontend.

The demonstration tenant is http://johnfashion-001.localhost:8080.
Its `/login` and `/backoffice` paths select the backoffice. If your browser or
OS does not resolve that hostname, add this entry to your hosts file:

```text
127.0.0.1 johnfashion-001.localhost
```

The route-verification script supplies Host headers itself, so it does not need
a hosts-file change. The hostname is a routing demonstration, not a tenant record.

Container services:
- `api`: the only backend host, running as the .NET non-root application user.
- `postgres`: PostgreSQL with a named volume and readiness check.
- `storefront`, `backoffice`, `admin`: built React files served by Nginx.
- `gateway`: local same-origin host/path routing.

Swagger is disabled in the Production container environment. API port 5080 and
gateway port 8080 are bound to loopback for local use. Frontend containers are
internal; access them through the gateway.

```powershell
docker compose logs api
docker compose stop
docker compose down
```

`down` preserves the database volume. Do not add `-v` unless you intend to
delete that database. Changing the password in `.env` does not change the
password of a database already initialized in the volume.

The dedicated local subnet is `172.30.71.0/24`; the gateway is `172.30.71.10`.
If that subnet conflicts with another network, change both Compose IPAM and
`ReverseProxy__KnownProxies__0` together.

## Visual Studio

1. Open `TailorSpadi.sln` in a Visual Studio installation supporting .NET 10.
2. Run the initialization script and start PostgreSQL using the commands above.
3. Set `TailorSpadi.Api` as the startup project and select the `http` profile.
4. Press F5. Start each React shell with its npm command in a terminal.
5. Run unit/API tests in Test Explorer. Tests tagged `Category=Database`
   require the Docker Linux engine and use their own disposable PostgreSQL.

Frontend shells are independent npm workspaces, not extra backend hosts.

## Project layout

```text
TailorSpadi.sln
Directory.Build.props          Nullable, analyzers, warnings as errors
.editorconfig                  C# and text conventions
global.json                    .NET SDK baseline
.config/dotnet-tools.json      Local EF command-line tool
src/
  Backend/
    TailorSpadi.Api/            Endpoints, errors, composition, configuration
    TailorSpadi.Application/    Features/SystemInfo, behaviors, contracts, time
    TailorSpadi.Domain/         Intentionally empty business layer
    TailorSpadi.Infrastructure/ Persistence and PostgreSQL health check
  Frontend/
    Dockerfile                 Builds one selected frontend workspace
    tailorspadi-storefront/
    tailorspadi-backoffice/
    tailorspadi-admin/
tests/
  TailorSpadi.UnitTests/
  TailorSpadi.IntegrationTests/
deploy/nginx/                  Gateway routing and trusted forwarding
scripts/                       Local setup and route verification
docs/                          Coding rules, decisions, verification record
compose.yaml
.env.example
package.json
package-lock.json
```

The API references Application and Infrastructure. Infrastructure references
Application and Domain. Application references Domain. Domain has no project or
framework dependencies. Dependency injection is composed in `Program.cs`.

Queries and commands use the same database. The scoped mediator invokes
logging, validation, then the handler. Scoped lifetime allows future handlers
to use the scoped DbContext. There is no custom mediator wrapper, generic
repository, transaction pipeline, or separate read store.

The [Mediator library](https://github.com/martinothamar/Mediator) is MIT licensed.
Its source generator lives only in the API; messages and handlers use its
abstractions in Application. The build and dispatch tests verify .NET 10 use.

## Configuration

| Setting | Purpose |
| --- | --- |
| `ConnectionStrings:Postgres` | Required connection string, from user secrets or environment |
| `BusinessTime:DefaultTimeZone` | Defaults to `Africa/Lagos` |
| `AllowedHosts` | Semicolon-separated accepted API hostnames |
| `Cors:AllowedOrigins` | Explicit development origins; empty in Production |
| `ReverseProxy:KnownProxies` | Explicit trusted proxy IP addresses |
| `ASPNETCORE_ENVIRONMENT` | Development enables Swagger and local CORS origins |

Environment variables use double underscores, e.g.
`ConnectionStrings__Postgres`. Docker Compose reads `.env`; `dotnet run`
does not. The initialization script bridges local credentials through user secrets.

The database context requires configuration but does not connect at startup. Metadata and
liveness remain available during a database outage; readiness returns 503.
Health responses contain only status text.

Production browser traffic should use the same origin through a trusted edge,
so CORS remains disabled by default. If another origin is needed, explicitly
list it after validation. Do not combine wildcard origins and credentials.

## Dates and times

- Inject `TimeProvider` for the UTC clock. Normalize offset-bearing inputs to UTC.
- Persist event instants as PostgreSQL `timestamptz`; expose UTC ISO 8601 with `Z`.
- Persist calendar dates as `date` / `DateOnly` / `YYYY-MM-DD`.
- `BusinessTime` converts business-local appointments and local-day boundaries
  using the named zone. Local appointment input must have unspecified DateTime kind.
- Each shell has a small `time.ts` helper that formats explicitly in Lagos.
- Daily queries use `start <= timestamp && timestamp < end`.
- Reject ambiguous/invalid local times instead of silently guessing.
- Tenant-specific time zones and deadline rules arrive with their business modules.

## Migrations

There are no business entities or migrations in Module 1. Do not create an empty
migration just to demonstrate tooling. EF configuration belongs in Infrastructure.

After adding the first real entities and mappings in a later module:

```powershell
dotnet tool restore
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet ef migrations add InitialBusinessSchema --project src/Backend/TailorSpadi.Infrastructure --startup-project src/Backend/TailorSpadi.Api --output-dir Persistence/Migrations
dotnet ef database update --project src/Backend/TailorSpadi.Infrastructure --startup-project src/Backend/TailorSpadi.Api
```

For deployment, generate and review an idempotent migration script in CI:

```powershell
dotnet ef migrations script --idempotent --project src/Backend/TailorSpadi.Infrastructure --startup-project src/Backend/TailorSpadi.Api --output migrations.sql
```

Apply it as an explicit deployment step with deployment credentials before
switching traffic. The API never calls `EnsureCreated` or `Migrate` on startup.
Back up before schema changes and plan application/schema compatibility for rollback.

## Verification commands

```powershell
dotnet restore TailorSpadi.sln --locked-mode
dotnet build TailorSpadi.sln --no-restore
dotnet format TailorSpadi.sln --verify-no-changes --no-restore
dotnet test TailorSpadi.sln --no-build
npm ci
npm run build
npm test
./scripts/Verify-Routes.ps1
```

For a focused run without Docker:
`dotnet test TailorSpadi.sln --no-build --filter "Category!=Database"`.
This deliberately excludes the real PostgreSQL test; it is not full verification.

Optional Windows browser checks: run `./scripts/Verify-Browser.ps1 -Local` while the local servers are running, or `./scripts/Verify-Browser.ps1` for Docker. These launch headless Edge and verify rendered React content, including the live API response.

See [package versions](docs/versions.md), [verification results](docs/verification.md), [coding standards](docs/coding-standards.md),
and [routing and Module 2 decisions](docs/architecture.md).
