# Module 2 testing guide

Use this checklist to accept identity, onboarding and tenancy. Each case includes
steps and the expected result. This is a test plan, not a record of a new test run.
For the last recorded results, see [verification.md](verification.md).

## 1. Prepare the environment

1. Start Docker Desktop and open PowerShell in the repository root.
2. For an already initialized environment, run `docker compose up -d --wait`.
   For a fresh setup or updated source, follow [README.md](../README.md), including
   certificate trust, migrations and `docker compose up --build -d --wait`.
3. Open `https://tailorspadi.localhost:8443` in Edge or Chrome. The browser must show
   no certificate warning. Do not bypass certificate errors for authentication tests.
4. Use two separate browser profiles: one for owners and one for customers. Separate
   tabs share cookies and do not represent separate sessions.
5. Prepare unique test emails, such as `owner-a-<run>@example.test`,
   `owner-b-<run>@example.test` and `customer-<run>@example.test`. Choose test-only
   passwords of 10–128 characters that are not common passwords or the email address.
6. Prepare a small PNG, JPEG and WebP; an SVG; a text file renamed to `.png`; and an
   image larger than 2 MiB. The default decoded image limit is 16 million pixels.
7. Record the website URLs returned when creating businesses. Below, **Platform**
   means `https://tailorspadi.localhost:8443`; **Tenant A/B** means those returned URLs.
   Never guess the numeric suffix assigned to a business.

Run the browser cases below in order. Keep the test businesses until all checks
finish. Manual test accounts remain in the local database; the automated browser
journey removes only its own generated records.

## 2. Browser acceptance checklist

### M01 — Routes and unauthenticated access

1. Open Platform `/`, `/register`, `/login` and `/admin/login`.
2. In a signed-out profile, open Platform `/onboarding`.
3. Open `https://nobody-here.tailorspadi.localhost:8443/`.

**Expected:** Correct pages load; protected onboarding requires sign-in; the unknown
business shows “Store not found”. There are no broken assets or certificate errors.

### M02 — Tailor registration and validation

1. Open Platform `/register` and submit empty fields.
2. Try an invalid email, a password shorter than 10 characters, and mismatched
   password confirmation. Try a common password as a separate check.
3. Enter valid details for Owner A and select **Create account**.
4. Sign out and attempt another registration using the same email with different
   letter casing.

**Expected:** Invalid data is rejected with clear field/server errors. Valid
registration leads to onboarding. The duplicate email does not create a second user.
Browser-native email validation may appear before an API request is sent.

### M03 — Sign-in, session restoration and sign-out

1. Sign in at Platform `/login` using a wrong password; repeat with an unknown email.
2. Sign in using Owner A's correct credentials.
3. Reload the page and open another tab on the same platform host.
4. Sign out, then revisit a protected page and reload it.

**Expected:** Failed sign-ins use a generic error. Correct sign-in succeeds and
survives reload through refresh. After sign-out, protected data is inaccessible.
An already open tab may retain a rendered view until navigation or its next request;
that is not proof its server session remains valid.

### M04 — Business information and draft ownership

1. Sign in as Owner A and open `/onboarding`.
2. Submit missing required business name, phone, email or country.
3. Save valid details using business name `John & Sons Fashion`.
4. Reload after selecting **Continue**.
5. In the second profile, register Owner B and open onboarding without saving a draft.

**Expected:** Invalid details do not advance the wizard. Saved details persist and
Owner A resumes at branding. Owner B cannot see Owner A's draft.
Only saved steps are guaranteed to survive reload; unsaved edits are not autosaved.

### M05 — Branding, logo validation and saved preview

1. In Owner A's branding step, select primary and secondary colors.
2. Upload a valid PNG, then JPEG or WebP, and check the preview.
3. Try the SVG, renamed text file and image larger than 2 MiB.
4. Save valid branding with **Continue**, reload, then go back to branding.

**Expected:** Color and logo previews work; invalid files show a useful error and
preserve the previously saved logo. Saved branding persists. Accepted images are
re-encoded to PNG and stored at a maximum edge of 512 pixels. Exact decoded-size
and cleanup behavior is also covered by the automated upload tests.

### M06 — Subdomain selection and review resume

1. Continue to the address/subdomain step.
2. Check the suggested base derived from `John & Sons Fashion`.
3. Try a reserved name such as `admin`, then a valid unique address name.
4. Wait for the availability check, select **Continue**, and reload the review page.
5. Go back, choose another valid name, save, and reload again.

**Expected:** The derived base is `john-and-sons-fashion`. Reserved/invalid names
are rejected. The latest saved choice and review step survive reload. The preview
is a suggestion; completion makes the final allocation with a three-digit suffix.

### M07 — Launch and platform-to-tenant handoff

1. Review the saved details and select **Launch my business**.
2. Record the returned website and backoffice URLs as Tenant A.
3. Open the website, then the backoffice sign-in link.
4. Sign in on Tenant A using Owner A's credentials.

**Expected:** Completion shows “Your TailorSpadi website is ready.” Tenant A displays
its business details, branding and logo. It requires its own sign-in even though
Owner A signed in on Platform. Successful tenant sign-in opens **Your workspace**.
Tokens never appear in the address bar. Catalog and ordering remain placeholders.

### M08 — Multiple businesses and independent ownership

1. Return to Platform as Owner A and check the business list at `/businesses`.
2. Start another onboarding flow at `/onboarding` and complete a second business.
3. Complete Owner B's onboarding in the second profile and record its URL as Tenant B.
4. Try signing in to Tenant B's `/login` using Owner A's credentials.

**Expected:** Owner A can own multiple businesses and sees the correct list. Owner B's
business remains separate. Owner A is denied tenant access without membership in B.
A platform business list must never include another owner's businesses.

### M09 — Replace a live business logo

1. Sign in as Owner A on Tenant A `/login` and open `/backoffice`.
2. Use **Replace logo** to upload a different valid image.
3. Reload the workspace and public tenant website.
4. Try replacing it with an invalid file.

**Expected:** The new logo persists and appears publicly; invalid replacement leaves
the valid logo intact. Old-file deletion and failed-save cleanup are verified by
integration tests, not by the browser preview alone.

### M10 — Customer registration and role restrictions

1. In a fresh customer profile, open Tenant A `/register-customer`.
2. Test empty/invalid fields, then register the customer test account.
3. Sign out and sign in again through Tenant A `/customer/login`.
4. Open Tenant A `/customer`, then try `/backoffice`.

**Expected:** Customer registration and sign-in succeed; the customer account page
loads. A Customer cannot use the business workspace or owner-only logo operations.
Reusing an email already registered globally must not silently create another user.

### M11 — An existing customer joins another tenant

1. As the customer from M10, open Tenant B `/customer/login`.
2. Enter incorrect credentials first, then the correct credentials.
3. When offered, select **Join this store**.
4. Sign out and sign in on B again; then verify A still accepts that customer's login.

**Expected:** Joining requires valid authentication. The same global user gains a
Customer membership in B and retains membership in A. Joining does not grant an
owner/staff role or create a duplicate global user.

### M12 — Password recovery screens

1. Open Platform `/forgot-password` and submit a known test email.
2. Repeat with an unknown email.
3. Open `/reset-password?token=invalid-test-token`.
4. Confirm the token disappears from the address bar. Submit otherwise valid new
   password fields; also check mismatched confirmation.

**Expected:** Forgot-password responses do not reveal whether an account exists.
Invalid reset tokens cannot change a password. Confirmation errors are clear.

#### Enable email previews in Docker (no SMTP required)

1. Start Docker Desktop. Open PowerShell in the repository root:

   ```powershell
   Set-Location C:\Users\BELLOTECH\source\repos\TailorSpadi
   ```

2. Apply the local email-debug override and wait for the services to become healthy:

   ```powershell
   docker compose -f compose.yaml -f compose.email-debug.yaml up --build -d --wait
   ```

   This runs the API in **Development** and sets `Email__ShowInConsole=true`.
   Both settings are required. The normal Compose configuration uses Production
   and does not print email contents. Existing local database data is retained.

3. Follow new API output in PowerShell:

   ```powershell
   docker compose logs --follow --tail 0 api
   ```

   Keep this terminal open before submitting the request. Alternatively, open
   Docker Desktop → Containers → the TailorSpadi API container → Logs.

4. In the browser, sign out if necessary and open
   `https://tailorspadi.localhost:8443/forgot-password`. Submit the email of an
   existing, active, non-admin test account.

5. Find the output headed `--- LOCAL TEST EMAIL (not sent) ---`. It contains the
   recipient, subject, message and password-reset URL. Copy only the full URL
   into the browser, excluding Docker's log prefix. No real email is sent.

6. Set a new password. Verify the old password fails and the new password works.
   Reopen the same reset link and attempt another reset: it must be rejected.
   Links expire after 30 minutes; request a new link if needed.

7. Repeat from Tenant A `/forgot-password` and verify the reset URL keeps the
   tenant hostname. Existing sessions should be rejected on their next protected
   request/refresh after a successful reset. Use the automated expiry tests instead
   of waiting 30 minutes manually.

**If no email appears:** Check that you started Compose with both files, follow the
API container's output (not the frontend or browser console), and submit a fresh
request for an existing active non-admin account. Unknown emails intentionally return
the same generic response without generating a message. To view recent output, use
`docker compose logs --tail 100 api`.

#### Turn Docker email previews off

1. Press **Ctrl+C** to stop following logs; this does not stop the containers.
2. Restart using only the normal Compose configuration:

   ```powershell
   docker compose -f compose.yaml up -d --wait
   ```

   The API returns to Production mode, which never registers the preview adapter.
   This does not erase earlier Docker log entries. Reset links are sensitive; do
   not include them in screenshots, shared logs or bug reports.

For Visual Studio testing, run the API in Development and use **Output → Debug**.
Real inbox delivery remains deferred to Module 7.

### M13 — Admin separation

1. Open Platform `/admin/login` and try ordinary owner/customer credentials.
2. Try opening Platform `/admin` without an admin session.
3. If an administrator has explicitly been provisioned through the documented secret
   configuration, sign in with that account and open the protected placeholder.

**Expected:** Ordinary accounts cannot access admin. A provisioned admin can access
only the implemented admin placeholder; management dashboards are deferred.
There are no default administrator credentials. Record the successful admin login
case as **Blocked** if no admin was provisioned; do not count it as passed.

### M14 — Cookies and browser storage

1. Sign in on Platform and Tenant A, then open browser DevTools → Application.
2. Inspect Cookies for each host: platform uses `ts_rt_p`, tenant uses `ts_rt_t`;
   an admin session uses `ts_rt_a`.
3. Check the cookies have HttpOnly, Secure and SameSite=Strict, and are host-only
   rather than shared through a parent-domain cookie.
4. Inspect Local Storage and Session Storage on each host and inspect navigation URLs.
5. Sign out of Tenant A and check its refresh cookie is cleared.

**Expected:** Credentials/tokens are absent from browser storage and navigation URLs.
Refresh cookies stay scoped to their host. Signing out of the tenant does not
implicitly sign the user out of a separate platform session. Never put token values
or passwords into screenshots or bug reports.

### M15 — Error recovery, keyboard use and small screens

1. Use the keyboard to navigate registration, sign-in and each wizard step.
2. Check labels, error messages, focus visibility and layout at a narrow mobile width.
3. In DevTools Network, switch offline while saving an unlaunched draft step.
4. Restore connectivity and retry; then reload a previously saved step.

**Expected:** Controls remain usable, errors are readable, requests show a busy state,
and failures do not falsely advance the wizard. Retrying works and saved data remains.
Do this on a draft, not during final launch, unless explicitly testing completion recovery.

## 3. API, security and database checks

Use the existing automated tests for the following cases. These checks are required
for Module 2 even when the browser checklist passes. They use disposable PostgreSQL
databases and apply the real migrations. Run from the repository root with Docker up.

To run one group, replace `<ClassName>` in this command with a class from the table:

```powershell
dotnet test tests/TailorSpadi.IntegrationTests --filter "FullyQualifiedName~<ClassName>"
```

| Group / class | Steps exercised | Required result |
| --- | --- | --- |
| `IdentityApiTests` | Register normalized/duplicate emails; sign in; rotate and reuse cookies; logout; reset/change passwords; omit CSRF header or change Origin | Correct validation; generic credential errors; revocation enforced; bad CSRF rejected; four simultaneous refreshes yield one success and three unauthorized responses |
| `IdentityExpiryTests` | Advance the injected clock past reset-token, refresh-token and lockout durations | Reset expires after 30 minutes; refresh after 14 days; temporary lockout ends after its configured period |
| `OnboardingApiTests` | Save/reload drafts; submit incomplete completion; complete repeatedly and concurrently; allocate identical business names | No incomplete activation, duplicate tenant or duplicate subdomain; idempotent completion |
| `ModuleTwoCompletionTests` | Replay concurrent refreshes; change security stamp; save review; change tenant status as admin/non-admin; attempt invalid relationships and cross-tenant writes | Family revocation, saved review, correct cache invalidation, admin-only status changes and database/write guards enforced |
| `TenantIsolationTests` | Use tenant A identity/data against B | Cross-tenant access rejected; query/write isolation holds |
| `TenantResolutionTests` | Resolve known, unknown and inactive hosts; exercise malformed/untrusted hosts and cache behavior | Host alone determines tenant; unknown/incomplete is 404, suspended/deactivated is 403; no forwarded-host bypass |
| `AdminSeparationTests` | Mix admin, platform and tenant accounts/audiences | No cross-scope privilege escalation; guarded admin provisioning |
| `LogoUploadTests` | Send valid/invalid formats, oversized/invalid images and unauthorized uploads | Only valid authorized images saved; safe public delivery |
| `LocalFileStorageTests` | Cancel storage writes | Temporary files cleaned up |
| `MigrationTests` | Apply migrations to an empty database and compare EF model | Migration succeeds; no pending model changes |
| `RateLimitTests` | Exceed configured test login request limit | 429 with Retry-After |
| `PostgresTests` | Connect to real PostgreSQL and round-trip dates | Readiness succeeds; UTC instants and calendar dates are preserved |
| `ApiTests` | Request public metadata and health endpoints with unavailable database | Liveness/system-info remain independent of database availability; readiness reports failure correctly |

Do not manually wait 14 days or edit production records to exercise expiry. These
tests control the clock and database safely. Tenant activation/suspension commands
and password change are tested here; the current UI has no general status-management
screen or change-password screen. See [module-2-api.md](module-2-api.md) for endpoint
payloads if additional API testing is needed.

## 4. Full regression commands

Run these sequentially. Let builds finish before starting browser/database tests;
concurrent builds caused local resource contention during earlier verification.

```powershell
dotnet restore TailorSpadi.sln --locked-mode
dotnet build TailorSpadi.sln --no-restore
dotnet format TailorSpadi.sln --verify-no-changes --no-restore
dotnet test TailorSpadi.sln --no-build
npm ci
npm run build
npm test
npm audit
dotnet list TailorSpadi.sln package --vulnerable --include-transitive
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Verify-Routes.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Verify-Browser.ps1
npm run test:e2e
```

**Expected baseline from the last recorded run:** 104 C# unit tests, 70 integration
tests, 29 frontend tests, 10 route checks, 5 rendered-page checks and 1 browser journey
pass. Builds/format checks succeed and dependency audits report no known vulnerabilities.
The end-to-end test requires installed Edge, the trusted local certificate and the
running Docker stack. It covers registration → saved review/reload → launch → tenant
sign-in/reload, checks cookie/storage behavior and cleans up its generated records.

## 5. Record results and decide acceptance

Use one row per case; attach redacted evidence for failures.

| Case | Result: Pass / Fail / Blocked / Not run | Actual result or issue link |
| --- | --- | --- |
| M01 | Not run | |
| M02 | Not run | |
| M03 | Not run | |
| M04 | Not run | |
| M05 | Not run | |
| M06 | Not run | |
| M07 | Not run | |
| M08 | Not run | |
| M09 | Not run | |
| M10 | Not run | |
| M11 | Not run | |
| M12 | Not run | |
| M13 | Not run | |
| M14 | Not run | |
| M15 | Not run | |
| Automated regression | Not run | |

A failure report should contain case ID, browser/version, route, steps, expected and
actual behavior, timestamp, HTTP status/trace ID when available, and redacted evidence.
Do not include passwords, cookies, access/reset tokens or signing secrets.

Accept Module 2 when required checks pass, blockers are recorded and no unresolved
identity, data-loss or tenant-isolation issue remains. Email delivery, catalog,
orders, payments, measurements, staff invitations and management dashboards are
outside this module; do not report their absence as a Module 2 regression.
