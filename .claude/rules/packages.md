---
paths:
  - "Directory.Packages.props"
  - "Directory.Build.props"
  - "global.json"
  - "**/*.csproj"
  - "**/packages.lock.json"
  - ".config/dotnet-tools.json"
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
- Repo-local tools (`dotnet-ef`, `jb`, `maui`) are in `.config/dotnet-tools.json`, restored with `dotnet tool restore` after a fresh
  clone.