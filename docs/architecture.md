# Architecture and Module 2 decisions

TailorSpadi is a layered modular monolith: one API, one PostgreSQL database and
three React applications. Domain has no framework dependencies. Application holds
feature commands/queries, validators and the persistence contract. Infrastructure
implements EF mappings, hashing, tokens, storage and tenant resolution. API endpoints
remain thin. There is no generic repository, custom mediator or transaction pipeline.

## Identity and authorization

Users are global; normalized email is unique. TenantMembership joins a user and a
business with an active flag and a static role. TenantOwner can ManageBusiness;
customer memberships cannot access the business workspace. PlatformAdmin is a
separate account scope, audience and login. Provisioning is opt-in with no defaults.
Account and membership status commands authorize their callers. Tenant status changes
require an administrator and invalidate cached resolution after commit. Management
endpoints/UI are deferred because Module 2 admin is a protected placeholder.

Access JWTs last 15 minutes and live in memory. Refresh tokens contain 256 random
bits; only hashes, scope, security stamp, family and replacement metadata persist.
Rotation uses optimistic concurrency. Reuse, including concurrent replay, revokes
the family. Password reset/change and account deactivation invalidate old security
stamps. Password change preserves only the current refresh family, with its new stamp.
Tenant/user/membership status is checked before session use. Tenant deactivation
revokes its refresh tokens. Revoked refresh sessions cannot return after reactivation.

Cookies are host-only, HttpOnly, Secure and SameSite=Strict. Platform, tenant and
administrator scopes use separate names. Refresh/logout require the custom
X-Requested-With header and exact accepted Origin. Host handoff requires a fresh
login on the destination tenant; no tokens are passed in URLs or persistent browser
storage. Reset tokens are removed from the browser address bar when read. Normal
structured logs omit tokens and email bodies. For local testing, Development can
print full email previews to the console/debugger with `Email:ShowInConsole=true`.
Production never registers that adapter; real email delivery is deferred to Module 7.

## Tenancy and persistence

Only the accepted hostname determines the tenant. Nginx passes Host and replaces
forwarded protocol/address; the API trusts only configured proxy addresses and
ignores X-Forwarded-Host. Gateway/API domain configuration is aligned through
TENANCY_BASE_DOMAIN. Host validation does not establish tenant existence: database
resolution returns 404 for unknown/incomplete and 403 for suspended/deactivated.
Positive and negative cache entries have bounded size/lifetime and explicit invalidation.

Membership reads use a query filter. Handlers also authorize/predicate requests;
SaveChanges guards reject cross-tenant writes. Global onboarding accesses a draft
only through the authenticated owner's link. Tenant refresh records have a composite
foreign key to membership, and a check constraint binds scope to tenant presence.
Audit records reference valid users/tenants. Assigned subdomains cannot be changed
through EF writes. Direct database administration remains a privileged boundary.

One user may own several businesses and have one unfinished draft. Business details,
branding and the selected slug/review step persist. Completion performs one atomic
SaveChanges for tenant activation, owner membership and audit. Unique constraints
and bounded retries resolve concurrent suffix allocation and double submission.
Slug normalization retains the meaning of '&' as 'and'; server allocation appends a
three-digit suffix, rejects reserved names and reports exhaustion.

Schema changes are deployment steps. Startup never calls Migrate or EnsureCreated.
The committed initial migration and relationship migration are tested against empty
PostgreSQL databases and compared with the current model.

## Files and frontend

Logos are decoded within byte/pixel limits, re-encoded to PNG and stored with random
keys outside publicly served files. Only the owning draft user or active business
owner can upload. Failed database changes remove new files; replacement removes old
files, with bounded filesystem retries. Persistent storage errors are visible rather
than silently swallowed. Use backups and disk monitoring for deployed storage.

The shared frontend package exists to avoid duplicating session restoration,
refresh queuing, HTTP error mapping, guards and forms across the three apps.
React Hook Form manages fields; TanStack Query manages server state. Date/time
helpers retain Africa/Lagos presentation; backend instants use TimeProvider and UTC.

## Deployment limits

Compose is a local HTTPS setup. Production additionally needs real DNS/certificates,
secret management, storage/backups, monitoring, a real email adapter and reviewed
proxy/host configuration. Redis, brokers, workers, event sourcing, separate read
stores and microservices are not required for this module. Catalog, measurements,
orders, payments, branches, invitations and management dashboards remain deferred.
