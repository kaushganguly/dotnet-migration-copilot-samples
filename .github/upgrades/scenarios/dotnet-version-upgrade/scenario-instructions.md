# Scenario Instructions — dotnet-version-upgrade

## Scope
- Solution: /home/runner/work/dotnet-migration-copilot-samples/dotnet-migration-copilot-samples/ContosoUniversity/ContosoUniversity.sln
- Project: /home/runner/work/dotnet-migration-copilot-samples/dotnet-migration-copilot-samples/ContosoUniversity/ContosoUniversity.csproj
- Target Framework: net10.0
- Source: .NET Framework 4.8 (ASP.NET MVC 5, System.Web, legacy csproj, packages.config)
- Target: ASP.NET Core MVC on .NET 10.0, SDK-style project, PackageReference, EF Core

## Requirements (from task)
- Convert project to SDK-style format
- Migrate from ASP.NET MVC 5 (System.Web) to ASP.NET Core MVC
- Replace packages.config with PackageReference
- Update NuGet dependencies (including EF -> EF Core) to .NET 10-compatible versions
- Resolve resulting API compatibility issues

## Success Criteria
- passBuild: true
- generateNewUnitTests: false
- passUnitTests: true
- securityComplianceCheck: true

## User Preferences

### Flow Mode
**Automatic** — do not pause for user input/review at any stage (assessment, planning, breakdown,
or between tasks). Only pause if genuinely blocked (missing info, ambiguous decision with major
consequences, unrecoverable error). Surface assessment/plan/progress inline without waiting for
approval.

### Source Control
- Git repo: yes
- Working branch: upgrade/dotnet-net10 (created from main, no pending changes to stash)
- Commit strategy: commit after each completed task with descriptive messages

### Technical Preferences
- Target framework: net10.0 (confirmed from task request)
- No user review pauses required — proceed end-to-end autonomously

## Upgrade Options

### Strategy
- Upgrade Strategy: All-at-Once

### Project Structure
- Project Approach: In-place rewrite (Web Projects) — single project, 7 controllers, 1092 LOC, full rewrite requested by task

### Compatibility
- Unsupported Packages: Resolve Inline (ASP.NET MVC5-era packages — Microsoft.AspNet.Mvc/Razor/WebPages/Web.Optimization, Antlr, WebGrease, Modernizr, Microsoft.CodeDom.Providers.DotNetCompilerPlatform, Microsoft.Web.Infrastructure — removed/replaced with ASP.NET Core built-ins)
- Unsupported API Handling: Fix Inline (single project, in-place, few complex changes)
- Windows Native APIs: No Compatibility Pack (MSMQ/System.Messaging is the only Windows-only usage; replacing with ConcurrentQueue-backed in-process implementation for Linux/container compatibility)
- System.Web Adapters: Direct Migration to ASP.NET Core APIs (small project, in-place rewrite, low HttpContext.Current usage)

### Modernization
- Assembly Binding Redirects: Remove Binding Redirects (standard VS-generated redirects, no underlying conflicts; not needed on SDK-style net10.0)
- Nullable Reference Types: Leave Disabled (default; enable separately as a distinct future effort)

### Reliability
- Test Coverage: Not Generate (task success criteria explicitly sets generateNewUnitTests=false; no existing test projects)
