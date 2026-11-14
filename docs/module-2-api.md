# Module 2 API contract

JSON uses camelCase. Errors are RFC 7807 Problem Details
(`application/problem+json`) with `status`, `title`, `traceId`, and an optional
`code` extension. Validation failures are 400 with an `errors` map
(`{ "field": ["message"] }`, field names camelCase).

All state-changing cookie-authenticated calls (`refresh`, `logout`) must send the
header `X-Requested-With: TailorSpadi` and a browser `Origin` header equal to the
page origin (browsers add this automatically on same-origin POST).

Bearer calls send `Authorization: Bearer <accessToken>`. The access token lives
only in memory; the refresh token is an httpOnly cookie the browser handles.

## Hosts and scopes

| Host | Scope of a session | Refresh cookie |
| --- | --- | --- |
| Platform host (`tailorspadi.localhost` locally; configured production domain) | `platform` (owner onboarding) or `admin` | `ts_rt_p` / `ts_rt_a` |
| Tenant host (`<slug>.tailorspadi.localhost` locally; `<slug>.<configured-domain>` in production) | `tenant` | `ts_rt_t` |

A session is only valid on the host it was created on. No token crosses hosts.

## Shared shapes

```ts
type Scope = 'tenant' | 'platform' | 'admin';
type Role = 'TenantOwner' | 'TenantManager' | 'TenantStaff' | 'Customer' | 'PlatformAdmin';

interface SessionInfo {
  scope: Scope;
  user: { id: string; email: string; firstName: string; lastName: string; phone: string | null };
  role: Role | null;               // tenant: membership role; admin: 'PlatformAdmin'; platform: null
  tenant: { name: string; subdomain: string; websiteUrl: string } | null;   // tenant scope only
  businesses: Business[];          // platform scope only; otherwise []
  hasDraft: boolean;               // platform scope only: an unfinished onboarding draft exists
}
interface Business { name: string; subdomain: string; role: Role; websiteUrl: string; backofficeUrl: string; }

interface SessionResponse { accessToken: string; expiresInSeconds: number; session: SessionInfo; }
```

## Auth

| Method + path | Body | Success | Notes |
| --- | --- | --- | --- |
| `POST /api/auth/register-tailor` | `{firstName,lastName,email,phone,password,confirmPassword}` | 201 `{userId}` | platform host only; 409 `email_taken` |
| `POST /api/auth/register-customer` | same | 201 `{userId}` | tenant host only; 409 `email_taken` (then use `join`) |
| `POST /api/auth/login` | `{email,password}` | 200 `SessionResponse` + sets refresh cookie | 401 `invalid_credentials`; 403 `membership_required` on a tenant host when the account has no membership there |
| `POST /api/auth/join` | `{email,password}` | 200 `SessionResponse` | tenant host; authenticates, then adds a Customer membership |
| `POST /api/auth/refresh` | none (cookie) | 200 `SessionResponse` | 401 `invalid_session` and cookie cleared |
| `POST /api/auth/logout` | none (cookie) | 204 | always clears the cookie |
| `POST /api/auth/forgot-password` | `{email}` | 202 always | generic response |
| `POST /api/auth/reset-password` | `{token,newPassword,confirmPassword}` | 204 | 400 `reset_token_invalid` |
| `POST /api/auth/change-password` | `{currentPassword,newPassword,confirmPassword}` | 200 `SessionResponse` | bearer; other sessions revoked; 400 `wrong_password` |
| `GET /api/auth/me` | | 200 `SessionInfo` | bearer, any scope |
| `POST /api/admin/auth/login` | `{email,password}` | 200 `SessionResponse` (scope admin) | platform host only |
| `POST /api/admin/auth/refresh` | none (cookie) | 200 `SessionResponse` | |
| `POST /api/admin/auth/logout` | none (cookie) | 204 | |
| `GET /api/admin/auth/me` | | 200 `SessionInfo` | bearer, scope `admin` only (403 for other scopes) |

Rate limited endpoints answer 429 with `Retry-After`.

The reset link emailed to the user is
`<host-of-request>/reset-password?token=<token>`; the page reads the token, removes
it from the address bar (`history.replaceState`), then posts it.

## Onboarding (platform host, bearer with scope `platform`)

```ts
interface OnboardingState {
  hasDraft: boolean;
  draftId: string | null;
  step: 1 | 2 | 3 | 4;             // the next step to show; resume here
  slugBase: string | null;          // generated address confirmed before review
  business: { businessName: string; description: string; phone: string; whatsApp: string;
              email: string; address: string; city: string; state: string; country: string };
  branding: { primaryColor: string; secondaryColor: string; logoPreviewDataUrl: string | null };
}
interface CompleteResult { businessName: string; subdomain: string; websiteUrl: string; backofficeUrl: string; }
interface SubdomainPreview { slugBase: string; suggestedSubdomain: string; url: string; available: boolean; }
```

| Method + path | Body | Success |
| --- | --- | --- |
| `GET /api/onboarding` | | 200 `OnboardingState` (no draft: `hasDraft:false`, empty fields, step 1) |
| `PUT /api/onboarding/business` | `business` object | 200 `OnboardingState` (creates the draft on first save) |
| `PUT /api/onboarding/branding` | `{primaryColor,secondaryColor}` (`#RRGGBB`) | 200 `OnboardingState` |
| `POST /api/onboarding/logo` | multipart field `file` | 200 `OnboardingState`; 400 `invalid_image` |
| `POST /api/onboarding/subdomain/preview` | `{}` (derived from saved business name) | 200 `SubdomainPreview`; 409 `subdomain_taken`; 400 `reserved_name` / validation |
| `PUT /api/onboarding/subdomain` | `{}` | 200 `OnboardingState`; persists the generated address and review step; 409 `subdomain_taken` |
| `POST /api/onboarding/complete` | `{draftId}` | 200 `CompleteResult` (idempotent for the same draft) |

Business fields: `businessName` required (2-100), `phone` required, `email` required,
`description` optional (max 1000), `whatsApp`, `address`, `city`, `state`, `country`
(`country` required).

## Tenant (tenant host)

`GET /api/tenant/current` (public) →
`{name, description, subdomain, logoUrl: string|null, primaryColor, secondaryColor, phone, whatsApp, email, address, city, state, country}`.
404 `tenant_not_found`; 403 `tenant_suspended`.
`GET /api/tenant/current/logo` (public) returns the logo image.
`POST /api/tenant/logo` (multipart `file`; bearer with scope `tenant`, role TenantOwner) replaces the logo → 200 `{logoUrl}`.

## Password policy (client hints; server is authoritative)

At least 10 and at most 128 characters; common passwords are rejected; must differ from the email.

Legacy `slugBase` request values are ignored. Addresses always derive from the saved business name; duplicate addresses return `409 subdomain_taken`.
