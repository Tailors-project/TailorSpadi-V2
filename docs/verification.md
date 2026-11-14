# Module 2 verification — 11 November 2026

The downloaded implementation was integrated into `feature/module-2-integration`
and completed against `module-2-guidelines.md`. The Downloads folder was preserved.
This record supersedes the earlier Module 1 and downloaded sandbox results.

## Implemented and reviewed

- Global identity, separate platform/tenant/admin sessions, password recovery
  contracts, refresh rotation/reuse detection and security-stamp invalidation.
- Four-step resumable onboarding, saved address selection, atomic/idempotent
  completion, concurrent subdomain allocation and immutable assigned addresses.
- Host-based tenant resolution, bounded cache invalidation, tenant write guards,
  membership constraints and authorized status changes/session revocation.
- Validated/re-encoded logo storage, temporary/new-file cleanup and replacement.
- Shared frontend session handling, guarded routes, accessible forms, previews,
  loading/error states and logout cache clearing.
- Two real EF migrations, local HTTPS, wildcard tenant routing, persistent storage,
  secret initialization and pinned dependency lock files.

## Checks

| Check | Result |
| --- | --- |
| Locked NuGet restore | Passed |
| C# unit tests | 104 passed |
| PostgreSQL integration tests | 70 passed; no failures or skips (final run: 6 minutes 5 seconds) |
| Frontend tests | 29 passed |
| npm clean installation | Passed |
| Production frontend builds | All workspaces passed |
| C# formatting | Verified with no changes |
| Docker Linux build and health | Full stack and final API image passed; services healthy |
| HTTPS routes and browser checks | 10 route checks, 5 rendered pages and the full onboarding journey passed |
| npm vulnerability audit | No vulnerabilities reported |
| NuGet audit, including transitive packages | No vulnerabilities reported in six projects |

Integration tests apply committed migrations to disposable PostgreSQL databases.
They cover identity expiry/reuse, CSRF, authorization, tenant isolation, concurrency,
cache invalidation, uploads, database constraints and migration/model consistency.
An earlier run passed 69/70 tests; one database connection timed out before the
concurrent-onboarding test body. The fixture now avoids retaining a pool for each
isolated test database and allows a longer local Docker connection timeout.

The browser test creates a uniquely named account, reloads saved onboarding,
launches a business, signs in on its tenant host and reloads its session. It checks
secure host-only HttpOnly cookies and empty browser credential storage, then
removes its generated database records. Certificate errors are not ignored.
Edge rejected the original `*.localhost` certificate; local addresses now use
`tailorspadi.localhost` and `*.tailorspadi.localhost` with a trusted certificate.

## Local state and scope limits

The local database retains its volume and has both migrations applied. On this
machine PostgreSQL uses port 55432 because Windows reserves the original 54329.
No demo tenants or default administrator were seeded. Changes remain uncommitted;
nothing has been pushed or deployed remotely.

Real email delivery remains deferred to Module 7. At the time of this run, the development adapter logged
only delivery intent, never reset links or message bodies; recovery logic was
verified with a recording email adapter in integration tests. Admin management
screens and later business modules remain outside Module 2. Status commands are
implemented and tested without adding an unrequested management API.

Public DNS/TLS deployment, Visual Studio's interactive debugger/Test Explorer and
production operations were not exercised. Follow README for setup and reproduction.
Logs, browser output and TRX files are local ignored artifacts.

On 12 November, local email previews were added for Development via
`Email:ShowInConsole`. See README for console/debugger and Docker instructions.
The test results above predate that change.
