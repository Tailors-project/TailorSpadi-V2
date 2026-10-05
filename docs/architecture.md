# Architecture decisions and Module 2 handoff

## Established in Module 1

The solution is a layered modular-monolith foundation. Business modules will
be logical feature areas within these projects. There are no business-module
boundaries to enforce against real entities yet.

The API owns HTTP configuration and composition. Application owns use cases
and the small contracts needed by those use cases. Infrastructure owns EF
Core and external integrations. Domain stays framework-independent.

Mediator 3.0.2 is selected under its MIT license; the source generator is
installed only in the API. Handlers/pipelines use scoped lifetime, anticipating
EF Core and tenant-scoped request dependencies. No custom dispatch wrapper
or mediator dependency enters Domain.

One AppDbContext and one PostgreSQL database serve reads and writes.
No business entities, initial empty migration, or database abstraction is
invented for the system-information query.

Validation uses FluentValidation and maps to HTTP 400 Problem Details.
Unexpected failures return generic HTTP 500 Problem Details with a trace ID.
Shared request logging records message type, timing, and exception type only.

## Host routing and trust

The gateway selects a frontend using host and path before SPA fallbacks:
- Main host: storefront, except registration/onboarding and administration.
- Demonstration tenant: storefront, except login and backoffice.
- API and health paths always go to the backend.
- Unique storefront-assets, backoffice-assets, and admin-assets prefixes
  prevent frontend bundles from colliding.
- History fallback is configured in each frontend server.

The local gateway accepts only localhost, 127.0.0.1, and the one documented
demonstration tenant. Unknown hosts return 400. This is an allowlist for a
routing demonstration, not tenant resolution or authentication.

The gateway preserves Host, replaces forwarded-for/proto values, and removes
X-Forwarded-Host. The API accepts forwarding only from configured trusted
proxy addresses and checks AllowedHosts. It does not consume forwarded host.
No tenant ID supplied by the browser should become trusted tenant context.

## Production routing checklist

The supplied Compose topology is for local verification, not a completed
Internet deployment. Before deploying:
1. Configure the apex DNS record and wildcard DNS for *.tailorspadi.com.
2. Provision TLS for both the apex and wildcard names at the trusted edge;
   require HTTPS and choose HSTS policy there.
3. Replace local Nginx server names with production host rules and configure
   matching API AllowedHosts. A wildcard validates hostname syntax/domain,
   not whether a tenant exists.
4. Put the API behind the edge; restrict direct backend access. Configure
   explicit trusted proxy addresses and a bounded forwarding chain. Do not
   clear all proxy trust restrictions.
5. Resolve the business hostname against active tenant records in Module 2.
   Unknown or suspended tenants must fail; no fallback to another business.
6. Keep same-origin browser API routing. If cross-origin traffic is required,
   allow explicit reviewed origins; never use a credentialed wildcard.
7. Use a secret manager/environment for production credentials and a separate
   deployment migration step. Add backups, restore verification, and monitoring.

## Decide before Module 2

These decisions need short written answers, not additional Module 1 services:
- Can a user belong to more than one business? How is business switching handled?
- Are customer accounts global or tenant-specific? Is guest ordering in scope?
- What minimal owner/staff roles and invitation flows are required?
- Are branches in the first release?
- How are browser credentials stored and refreshed? Define JWT issuer,
  audience, signature, lifetime and revocation if JWTs are selected.
- Decide whether a server session/cookie design is preferable for these browser
  clients. Merely mentioning JWT does not settle secure storage or CSRF.
- Define host-scoped authentication, onboarding handoff, logout, and independent
  platform administrator authorization. Never move bearer tokens in URLs.
- Define tenant ownership on writes, scoped uniqueness, and relationships that
  cannot link data across tenants. Query filters alone are insufficient.
- Establish tests denying cross-tenant reads, writes, and relationship changes.

Current-user and tenant-context interfaces are contracts only. Module 2 must
provide trustworthy implementations before protected business endpoints exist.
No placeholder class claims to authenticate a user or resolve a tenant.

## Decide when the relevant feature arrives

- Map ownership of Catalog, Customers/Measurements, Orders/Production,
  Payments/Billing, and Notifications; keep module interaction explicit.
- Define order transitions and snapshots of agreed prices/details/measurements.
- Define transactional boundaries, staff-edit concurrency, duplicate submissions,
  payment callback idempotency, and reconciliation.
- Decide upload storage, privacy, access checks, and deletion with the media feature.
- Introduce background delivery/outbox only if reliable asynchronous work needs it.
- Define API pagination, currency precision, identifiers, and compatibility with
  the first relevant business endpoint.

Redis, brokers, event sourcing, workers, SignalR, separate read stores, full
permission engines, and microservices remain deferred.
