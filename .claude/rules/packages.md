---
paths:
  - "Directory.Packages.props"
  - "Directory.Build.props"
  - "global.json"
  - "**/*.csproj"
  - "**/packages.lock.json"
  - ".config/dotnet-tools.json"
  - "nuget.config"
  - ".nuget/**"
---
# Packages

- Versions live in `Directory.Packages.props` (Central Package Management), and a project names a package without a
  version. `Microsoft.Maui.Controls` alone takes `VersionOverride="$(MauiVersion)"` from the MAUI workload.
- EF Core, its Design and Relational packages and the `dotnet-ef` tool move as one version. The Npgsql provider and the
  `EFCore.*` plugins have versions of their own.
- `Microsoft.Maui.Core` matches the installed workload band (`dotnet workload list`).
- The preview Aspire integrations move in lockstep with the AppHost SDK.
- Every project restores with a lock file, and `packages.lock.json` changes go into the same commit as the package change.
- After a package leaves a referenced project, the consumers' lock files may still list it. Run
  `dotnet restore <consumer>.csproj --force-evaluate` on the AppHost and each test project, then check that the lock
  files only lose lines.
- A package built locally from a fix that awaits its upstream release lives in `.nuget/` with a prerelease version
  (`4.4.1-local.1`), mapped by name to the `local` source in `nuget.config`. A new local build takes the next prerelease
  number, since NuGet caches a package by its version. Once the fix is released, the version returns to nuget.org and
  the file leaves `.nuget/`. Today: `Aigamo.ResXGenerator`, combining
  [ycanardeau/ResXGenerator#15](https://github.com/ycanardeau/ResXGenerator/pull/15) and
  [ycanardeau/ResXGenerator#13](https://github.com/ycanardeau/ResXGenerator/pull/13) with a fix to the latter's
  placeholder detection.
- Repo-local tools (`dotnet-ef`, `jb`, `maui`) are in `.config/dotnet-tools.json`, restored with `dotnet tool restore` after a fresh
  clone.