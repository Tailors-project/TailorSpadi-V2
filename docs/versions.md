# Verified dependency versions

Recorded for Module 1 on 2026-09-30. Exact transitive versions are in each
project's packages.lock.json and the repository package-lock.json.

## Backend

| Package/tool | Version |
| --- | --- |
| Local .NET SDK | 10.0.201 |
| Target framework | net10.0 |
| Mediator.Abstractions / SourceGenerator | 3.0.2 |
| FluentValidation.DependencyInjectionExtensions | 12.1.1 |
| Microsoft.EntityFrameworkCore / Design / dotnet-ef | 10.0.5 |
| Npgsql.EntityFrameworkCore.PostgreSQL | 10.0.1 |
| Microsoft.Extensions logging/options/health packages | 10.0.5 |
| Serilog.AspNetCore | 10.0.0 |
| Swashbuckle.AspNetCore | 10.2.3 |
| Microsoft.AspNetCore.Mvc.Testing | 10.0.5 |
| Microsoft.NET.Test.Sdk | 17.14.1 |
| xunit | 2.9.3 |
| xunit.runner.visualstudio | 3.1.5 |
| Testcontainers.PostgreSql | 4.15.0 |

Mediator's [MIT license](https://github.com/martinothamar/Mediator/blob/main/LICENSE)
and [registration/pipeline documentation](https://github.com/martinothamar/Mediator)
were checked. The actual solution builds and dispatch tests verify compatibility.

Swagger and Testcontainers were updated after restore identified vulnerable
transitive dependencies. The final restore does not suppress NuGet audit warnings.

## Frontend (all three workspaces)

| Package | Version |
| --- | --- |
| React / React DOM | 19.2.4 |
| React Router DOM | 7.18.4 |
| TanStack React Query | 5.90.21 |
| Axios | 1.20.0 |
| Bootstrap | 5.3.8 |
| TypeScript | 5.9.3 |
| Vite | 7.3.6 |
| Vite React plugin | 5.1.4 |
| Vitest | 4.1.11 |
| React types | 19.2.14 |
| React DOM types | 19.2.3 |

Local verification used Node 22.17.0 and npm 11.19.1. The npm lock includes the
compatible esbuild security update. The final npm audit reported zero known
vulnerabilities at verification time.

## Containers

- mcr.microsoft.com/dotnet/sdk:10.0
- mcr.microsoft.com/dotnet/aspnet:10.0
- node:22-alpine
- nginx:stable-alpine
- postgres:17-alpine

These tags receive updates. Lock image digests as part of a production release
process; they are not immutable deployment pins in this local foundation.
