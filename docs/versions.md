# Package versions

The project targets .NET 10 (SDK baseline 10.0.201) and Node >=22.12.0.
Exact direct and transitive versions are in packages.lock.json and package-lock.json.

| Backend dependency | Version |
| --- | --- |
| Mediator | 3.0.2 |
| FluentValidation | 12.1.1 |
| EF Core / JwtBearer | 10.0.5 |
| Npgsql EF provider | 10.0.1 |
| SkiaSharp and Linux native assets | 3.119.0 |
| Serilog.AspNetCore | 10.0.0 |
| Swashbuckle.AspNetCore | 10.2.3 |

| Frontend dependency | Version |
| --- | --- |
| React / React DOM | 19.2.4 |
| React Router | 7.18.4 |
| TanStack Query | 5.90.21 |
| Axios | 1.20.0 |
| React Hook Form | 7.91.0 |
| Bootstrap | 5.3.8 |
| TypeScript | 5.9.3 |
| Vite | 7.3.7 |
| Vitest | 4.1.12 |
| Testing Library React | 16.3.0 |
| jsdom | 26.1.0 |
| Playwright Test | 1.65.0 |

Vitest was updated from the downloaded 4.1.11 to address the reported mocker
advisories. Registry installation reported zero npm vulnerabilities. See
verification.md for the final checks, rather than treating a historical audit as
an ongoing guarantee.

Container tags: postgres:17-alpine, nginx:stable-alpine, node:22-alpine,
mcr.microsoft.com/dotnet/sdk:10.0 and mcr.microsoft.com/dotnet/aspnet:10.0.
These are mutable local development tags; production releases should pin reviewed
digests. SkiaSharp native assets are verified in the Linux API image.
