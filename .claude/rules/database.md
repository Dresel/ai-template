---
paths:
  - "src/*.Data/**"
  - "src/*/*.Api/Features/**/*Handler.cs"
  - "tests/*.ArchitectureTests/**"
---
# Database

- Query handlers take `ReadOnlyAppDbContext`, command handlers `AppDbContext`, and neither takes `AppDbContextBase`.
- Every context gets its provider through `ConfigureAppDbContext` or `ConfigureReadOnlyAppDbContext`.
- Entities are plain classes. A business rule lives in the handler that needs it and answers a refusal with a case of
  its result union.
- A value constraint is an annotation on the entity: `[MaxLength]` on every string, `[Precision]`, `[Range]`. A decimal
  range is a `HasCheckConstraint` in the configuration.
- Keys, column types, relationships and indexes go into the entity's `IEntityTypeConfiguration`, which is listed by
  hand in `ApplyEntityConfigurations`.
- A new typed id needs its `[EfCoreConverter<T>]` in `VogenEfCoreConverters` and its sentinel in
  `ConfigureConventions`. A store-generated key also declares `ValueGeneratedOnAdd()`, and the entity initializes it
  with `Id.Unspecified`.
- Write each query as one method-syntax chain from the `DbSet` to its terminal operator. A list that searches, sorts and
  pages builds its filter once and picks the order in a switch.
- Value converters capture no state.
- Raw SQL and `HasCheckConstraint` use the snake_case database names.
- `ExecuteUpdate` and raw SQL on an `IAuditable` type set the audit columns themselves.
- The schema changes only through a migration, added with the AppHost stopped:
  `dotnet dotnet-ef migrations add <Name> --project src/FocusTemplate.Data --startup-project src/FocusTemplate.Data --context AppDbContext`.
- The dev seed implements both the sync and the async delegate, and each part checks for its own rows before inserting.
- No migration creates or grants to `focusdb_reader`.

Background: [docs/database.md](../../docs/database.md).