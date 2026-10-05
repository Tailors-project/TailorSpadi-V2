# Coding standards

Keep code straightforward enough that the next module can follow the
system-information feature as an example.

- Use PascalCase for types, namespaces, and public members; camelCase for
  parameters and local variables; _camelCase for private instance fields.
- Prefix interfaces with I. Name asynchronous Task-returning methods with Async;
  retain library-defined names when implementing their interfaces.
- Use file-scoped namespaces, four-space C# indentation, braces, and one main
  public type per file. Match the namespace to the project/folder.
- Keep nullable reference types enabled and resolve compiler/analyzer warnings.
  Do not disable warnings globally to make a build pass.
- Use clear names and small methods. Use var when the constructed type is clear;
  prefer explicit types when they improve readability.
- Use records for immutable request/response DTOs. Do not expose EF entities as
  API contracts.
- Keep endpoint handlers thin: bind the request, dispatch, and map the response.
  Keep use cases in Application and business invariants in Domain.
- Put a feature's messages, handlers, validators, and DTOs together. Avoid
  generic service/repository layers and interfaces without a real consumer.
- Define persistence contracts only when a handler actually needs one.
  Infrastructure implements them; Application must not reference Infrastructure.
- Use constructor injection. Register request-specific dependencies as scoped.
  Never capture a scoped DbContext in a singleton.
- Pass CancellationToken through handlers, database work, and HTTP requests.
  Do not block on async calls with Result or Wait.
- Use structured logging with fixed message templates. Do not log request
  bodies, tokens, passwords, connection strings, or customer measurements.
- Validation messages must be safe to show to a user and must not echo secrets.
- Use explicit transactions when several writes must commit together.
  Do not wrap every request in a transaction.
- Use decimal for money, with currency and precision rules decided alongside
  the feature. Use UTC instants and DateOnly as described in the README.
- Use small focused tests for business rules, integrations, and failure modes.
  Database compatibility checks use real PostgreSQL.
- TypeScript remains strict. Keep API calls in each app's client module.
  Do not add a shared component package until duplication warrants it.

Run `dotnet format TailorSpadi.sln --no-restore` before review, followed by
the build/tests. Exact NuGet and npm dependencies are recorded in lock files.
