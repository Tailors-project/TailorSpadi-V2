# TailorSpadi

Module 2 implements global identity, tailor/customer registration, business onboarding,
hostname-based tenancy and logo storage. It builds on the .NET 10 API, PostgreSQL 17
and three React applications from Module 1. Orders, catalog, measurements, payments,
staff invitations and management dashboards are deferred.

## Local setup (Windows / PowerShell)

Requires .NET SDK 10.0.201+, Node 22.12+, Docker Desktop's Linux engine and
OpenSSL (included with Git for Windows). Run from the repository root:

```powershell
./scripts/Initialize-Local.ps1
./scripts/Initialize-LocalTls.ps1 -Trust
docker compose up -d postgres
dotnet restore TailorSpadi.sln --locked-mode
dotnet tool restore
npm ci
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet ef database update --project src/Backend/TailorSpadi.Infrastructure --startup-project src/Backend/TailorSpadi.Api
docker compose up --build -d --wait
```

If Windows marks a copied script as downloaded, review it and use a process-only
execution policy override: `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Initialize-Local.ps1`.
Do not change the machine-wide execution policy.

The initialization script preserves the existing database password, adds a signing
key when upgrading Module 1, and configures .NET user secrets without printing
credentials. `.env`, private certificates, uploaded files and build output are ignored.

The TLS script generates a 90-day certificate for localhost, tailorspadi.localhost, *.tailorspadi.localhost and
127.0.0.1. `-Trust` installs this certificate in the current Windows user's trusted
root store. It is for this local environment only. Keep its private key private.
To renew, pass `-Renew -Trust` to the TLS
script, and restart the gateway. Remove the old certificate from the user's trusted
root store when it is no longer needed.

| Page/service | Address |
| --- | --- |
| Storefront | https://tailorspadi.localhost:8443 |
| Tailor registration | https://tailorspadi.localhost:8443/register |
| Platform sign-in / businesses | https://tailorspadi.localhost:8443/login /businesses |
| Onboarding | https://tailorspadi.localhost:8443/onboarding |
| Admin sign-in | https://tailorspadi.localhost:8443/admin/login |
| Created business | https://<assigned-subdomain>.tailorspadi.localhost:8443 |
| Business sign-in/workspace | Tenant host /login and /backoffice |
| Customer registration/sign-in | Tenant host /register-customer and /customer/login |
| Health | https://tailorspadi.localhost:8443/health/live and /health/ready |
| Public API metadata | https://tailorspadi.localhost:8443/api/system-info |

No demo tenants or default administrator credentials are seeded. Register and
complete onboarding to create a business. Unknown tenant hosts return 404;
suspended/deactivated tenants return 403. Browsers normally resolve *.tailorspadi.localhost to
loopback. Add explicit hosts entries for your created business if your OS does not.
HTTP port 8080 redirects accepted hosts to HTTPS; authentication verification uses HTTPS.

PostgreSQL is loopback-only. Its default port is 54329; change `POSTGRES_PORT` in
`.env` and rerun initialization if Windows reserves that port. This machine uses
55432. Check `netsh interface ipv4 show excludedportrange protocol=tcp` if Docker
reports a forbidden socket bind. Never delete the database volume to fix a port issue.

## Development and architecture

`TailorSpadi.sln` contains Api, Application, Domain, Infrastructure and two test
projects. Domain has no framework dependency. Application contains feature handlers,
validators and persistence contracts. Infrastructure implements those contracts with
EF Core, PostgreSQL, password hashing, token generation and local image storage.
API endpoints bind requests and dispatch through the scoped Mediator pipeline.

The React apps are npm workspaces. `tailorspadi-shared` contains the shared API client,
in-memory session store, refresh queue, route guards and form helpers. React Hook
Form handles forms; TanStack Query handles server state. The shared package's source
is compiled by each application.

For API debugging, stop the Docker API before running
`dotnet run --project src/Backend/TailorSpadi.Api`. Swagger is available at
http://localhost:5080/swagger in Development only. `npm run dev:storefront`,
`npm run dev:backoffice` and `npm run dev:admin` start Vite on 5173/5174/5175.
These HTTP servers are useful for isolated UI work; use the HTTPS Docker gateway
for host-scoped cookie and end-to-end authentication verification.

## Sessions, onboarding and storage

Access tokens last 15 minutes and stay in React memory. Refresh tokens are hashed
in PostgreSQL and rotate in host-only HttpOnly, Secure, SameSite=Strict cookies.
Platform, tenant and admin sessions have separate audiences and cookie names.
Refresh/logout require an exact accepted Origin and `X-Requested-With: TailorSpadi`.
Password/security-version changes invalidate old sessions. Reusing a rotated token
revokes its refresh-token family, including a simultaneous rotation conflict.

Platform owners can own multiple businesses but have one unfinished draft at a
time. Business information, branding and the selected slug/review step are saved.
Completion atomically allocates the unique subdomain, activates the tenant, creates
its owner membership and records an audit entry. Repeated completion returns the
same result. Assigned subdomains cannot be renamed.

Opening a tenant's backoffice requires signing in again on that hostname. Tokens
are never transferred through URLs or browser storage. Customer accounts are global;
joining another business requires authentication. Admin receives a separate login
and a protected placeholder, not a management dashboard.

Logo uploads accept decoded PNG/JPEG/WebP within byte/pixel limits and are re-encoded
to PNG. Keys are server-generated. Local debugging stores files under
`%LOCALAPPDATA%/TailorSpadi/storage`; Docker uses the `api-storage` volume. Failed
writes remove temporary files; failed database saves remove the new logo. Replaced
files are deleted with bounded retries, and persistent storage failures are surfaced.

## Database migrations and deployment

Committed migrations include `InitialIdentityAndTenancy` and
`EnforceIdentityRelationships`. The application never migrates or creates tables at
startup. Apply migrations explicitly before starting the new API version. The second
migration adds audit foreign keys and ensures tenant refresh sessions reference an
existing membership. A migration test checks an empty database and model consistency.

Generate a deployment script with:

```powershell
dotnet ef migrations script --idempotent --project src/Backend/TailorSpadi.Infrastructure --startup-project src/Backend/TailorSpadi.Api --output artifacts/migrations.sql
```

Back up existing data and review the script before applying it to any non-local
system. Production requires a real certificate/DNS, reviewed host configuration,
secrets, storage/backups and monitoring. This repository's Compose setup is local.

`TENANCY_BASE_DOMAIN` configures the gateway and API domain together. Replace the
local certificate for any other domain. The API accepts forwarded protocol/address
only from the configured proxy. `AUTH_SIGNING_KEY` must be a strong random secret;
startup rejects weak/missing keys and insecure production refresh-cookie settings.

Administrator provisioning is disabled by default. Explicitly configure
`Auth:AdminSeed:Enabled`, `Email` and a strong `Password` through secret configuration
for the initial provision, then disable it. Existing accounts are never overwritten.
Real email delivery is deferred to Module 7. In Development, `Email:ShowInConsole`
prints the recipient, subject and full message to the API console and, in Debug
builds, Visual Studio's Output window (select Debug). No SMTP server is needed.
This local preview includes password-reset links; do not share or collect this output.
Set `Email:ShowInConsole` to `false` to suppress message bodies. Production never
registers the development email adapter, even if this flag is set.

To enable previews in the local Docker stack:

```powershell
docker compose -f compose.yaml -f compose.email-debug.yaml up --build -d --wait
docker compose logs -f api
```

Register a test account, open `/forgot-password`, submit that email, then copy the
reset link from the API output into the browser. Unknown emails intentionally produce
no message. Links expire after 30 minutes and can be used once. Return to the normal
local configuration with `docker compose up -d --wait`; it uses Production mode.

## Verification

```powershell
dotnet restore TailorSpadi.sln --locked-mode
dotnet build TailorSpadi.sln --no-restore
dotnet format TailorSpadi.sln --verify-no-changes --no-restore
dotnet test TailorSpadi.sln --no-build
npm ci
npm run build
npm test
npm audit
./scripts/Verify-Routes.ps1
./scripts/Verify-Browser.ps1
npm run test:e2e
```

Database tests use disposable PostgreSQL containers and apply the real migrations.
The Playwright test uses installed Microsoft Edge and the trusted HTTPS gateway;
it removes its uniquely named account/business afterwards. It requires Docker
access for that cleanup. `E2E_BASE_URL` can override the local base URL.

See [architecture](docs/architecture.md), [API contract](docs/module-2-api.md),
[coding standards](docs/coding-standards.md), [Module 2 requirements](docs/module-2-guidelines.md)
and [verification results](docs/verification.md).
For manual acceptance steps and automated checks, use the [Module 2 testing guide](docs/module-2-testing-guide.md).
