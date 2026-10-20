# .NET Upgrade Plan: ContosoUniversity

## Overview

| Item | Value |
|------|-------|
| Source version | .NET Framework 4.8 (`v4.8`, legacy non-SDK-style csproj, ASP.NET MVC / System.Web) |
| Target version | .NET 10.0 (`net10.0`, latest LTS) |
| Solution | `ContosoUniversity.sln` |
| Projects | `ContosoUniversity` (`ContosoUniversity.csproj`) |

## Reason

The upgrade to .NET 10 LTS was explicitly requested. It moves the application off the Windows-only .NET Framework / System.Web stack onto a modern, long-term-supported, cross-platform runtime.

## Scope

A single upgrade task covers:

- Converting the project to SDK-style format and updating the target framework to `net10.0`.
- Migrating ASP.NET MVC (System.Web) hosting to ASP.NET Core (startup/pipeline, routing, configuration replacing `Web.config`/`Global.asax`, bundling and static assets).
- Replacing `packages.config` dependencies with `PackageReference` and updating NuGet packages to .NET 10 compatible versions (including data access, e.g. Entity Framework).
- Fixing API incompatibilities caused by the framework change.
- Verifying the build, unit tests and security compliance.

## Tasks

1. `001-upgrade-dotnet-to-net10` – Upgrade ContosoUniversity from .NET Framework 4.8 to .NET 10.0.
