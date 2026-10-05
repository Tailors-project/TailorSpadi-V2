# Module 1 verification

Verified on 2026-09-30 on Windows with .NET SDK 10.0.201, Node 22.17.0,
npm 11.19.1, Docker Desktop 4.68.0, and the Docker Linux engine.

## Results

| Check | Actual result |
| --- | --- |
| All six C# projects build | Passed; 0 warnings, 0 errors |
| C# formatting verification | Passed; dotnet format --verify-no-changes |
| C# unit tests on Windows | 10 passed |
| API integration tests without PostgreSQL | 5 passed |
| Isolated real PostgreSQL integration test | 1 passed |
| C# unit tests inside the Linux API build | 10 passed |
| TypeScript checks and production builds | All three frontend apps passed |
| Frontend date/time tests | 12 passed, four per app |
| npm dependency audit | 0 known vulnerabilities at verification time |
| Locked NuGet restore in Linux image | Passed without suppressed audit warnings |
| EF tool restore and migrations list | Passed; no migrations, as expected |
| Docker Compose configuration | Valid |
| API and three frontend image builds | Passed |
| Full Compose startup with --wait | Passed |
| Local API / health / Swagger / frontend route checks | Passed |
| Local rendered browser checks | Four URLs passed |
| Docker host/path/asset/API checks | Eleven checks passed |
| Docker rendered browser checks | Seven URLs passed |
| Database outage and recovery | Passed |

There are 16 distinct C# tests (10 unit, 6 integration). The Linux build repeats
the 10 unit tests; it does not introduce ten additional distinct tests.

## What was exercised

- The system-information handler and scoped mediator dispatch return public
  metadata without a tenant or a database connection.
- Validation prevents handler execution and becomes HTTP 400 Problem Details.
- Logging avoids private request values and exception messages.
- Cancellation passes through the pipeline and is honored by the handler.
- UTC-to-Lagos conversion crosses midnight correctly.
- Business appointments convert to UTC and daily boundaries use Lagos midnight.
- DateOnly JSON values remain unchanged.
- A real disposable PostgreSQL 17 container validates EF connectivity,
  timestamptz UTC round trips, and date round trips. No EF InMemory provider is used.
- Development Swagger exposes the system-information endpoint.
- Vite serves and refreshes nested routes, and proxies the storefront API call.
- Headless Edge renders each local app, including the storefront's actual API response.

Local browser URLs:
- http://127.0.0.1:5173/
- http://127.0.0.1:5174/login
- http://127.0.0.1:5174/backoffice/orders
- http://127.0.0.1:5175/admin/users

Container browser URLs:
- http://localhost:8080/
- http://localhost:8080/register
- http://localhost:8080/onboarding
- http://localhost:8080/admin/users
- http://johnfashion-001.localhost:8080/
- http://johnfashion-001.localhost:8080/login
- http://johnfashion-001.localhost:8080/backoffice/orders

The Docker route script also fetched each page's JavaScript/CSS assets,
verified metadata from both hosts, checked unknown.localhost returns 400,
and checked /api/missing returns 404 instead of frontend HTML.

During a controlled stop of this project's PostgreSQL container:
- /health/live returned 200.
- /health/ready returned 503.
- /api/system-info returned 200.
- After PostgreSQL restart, readiness returned 200 and the gateway was healthy.

## Handoff state

The full Docker stack is running at http://localhost:8080.
The local Vite/API development processes used for verification were stopped
to avoid conflicting with Docker's API port.

The initialized development database volume is retained. The ignored .env and
.NET user secrets are configured for this machine. Testcontainers disposed of
its separate test database container.

A local Git repository was initialized. No remote, commit, or publication was
created. Generated output, browser artifacts, test output, and .env are ignored.

## Checks not performed / scope limits

- Visual Studio F5 and the interactive Test Explorer were not manually exercised.
  The solution and all projects were built with the installed .NET SDK; the
  README provides Visual Studio setup instructions.
- Real DNS, public TLS, Internet deployment, authentication, and database-backed
  tenant resolution are outside Module 1 and were not claimed as tested.
- The tenant hostname is a routing fixture only. The placeholder pages contain
  no business data and do not implement authorization.
- No business migration was applied because this module has no business entities.
  EF may report that no mapping classes exist when inspecting the empty context.
- Module 2 decisions remain documented in architecture.md.

## Reproduce

From the repository root:

```powershell
dotnet restore TailorSpadi.sln --locked-mode
dotnet build TailorSpadi.sln --no-restore
dotnet format TailorSpadi.sln --verify-no-changes --no-restore
dotnet test TailorSpadi.sln --no-build
npm ci
npm run build
npm test
docker compose up --build -d --wait
./scripts/Verify-Routes.ps1
./scripts/Verify-Browser.ps1
```

Docker must be running for the real PostgreSQL test. Browser verification is
optional Windows/Edge tooling; route checks do not require a browser.

Test runner output is saved locally under TestResults. Browser DOM/log files
are under artifacts. These generated files are intentionally not committed.
