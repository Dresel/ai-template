---
paths:
  - "src/*.Data/**"
  - "src/*.Data.Foundation/**"
  - "src/features/*/*.Data/**"
  - "src/*/*.Api/Features/**/*Handler.cs"
  - "src/features/*/*.Api/**/*Handler.cs"
  - "src/admin/user-management/*.Api/**/*Handler.cs"
  - "tests/*.ArchitectureTests/**"
---
# Database

- Query handlers take a read-only context (`IReadOnlyDbContext`), command handlers a writable one, and neither takes
  a context's abstract base.
- A read-only context is its model base plus `IReadOnlyDbContext`, with no members of its own: `FoundationDbContext`
  refuses its `SaveChanges`.
- A context's model base derives from `FoundationDbContext<TModel>`, puts its ids into `ConfigureIds` and its
  configurations into `ConfigureModel`, and states its schema once, as `IHasSchema.Schema`.
- The spine's contexts get their provider through `ConfigureAppDbContext` or `ConfigureReadOnlyAppDbContext`, a
  feature's through `ConfigureFeatureDbContext` or `ConfigureReadOnlyFeatureDbContext`.
- A feature's entities go into its own data project, in a schema named after the feature, a product slice's into the
  product's context in `FocusTemplate.Data`, and neither into `SpatialFocus.Data`. A pooled context's only constructor
  parameter is its options.
- A feature reaches the spine's users through `UserReference` and saves nothing through it. Another feature's tables it
  reaches through ids and a lookup.
- Entities are plain classes. A business rule lives in the handler that needs it and answers a refusal with a case of
  its result union.
- A value constraint is an annotation on the entity: `[MaxLength]` on every string, `[Precision]`, `[Range]`. A decimal
  range is a `HasCheckConstraint` in the configuration.
- Keys, column types, relationships and indexes go into the entity's `IEntityTypeConfiguration`, which is listed by
  hand in the model's `Apply…Configurations`.
- A new typed id needs its `[EfCoreConverter<T>]` in the context's converter class (`VogenEfCoreConverters`,
  `WeatherEfCoreConverters`) and its sentinel in `ConfigureIds`. `UserId` comes from
  `AddFoundationConventions`. A store-generated key also declares `ValueGeneratedOnAdd()`, and the entity initializes it
  with `Id.Unspecified`.
- Write each query as one method-syntax chain from the `DbSet` to its terminal operator. A list that searches, sorts and
  pages builds its filter once and picks the order in a switch.
- Value converters capture no state.
- Raw SQL and `HasCheckConstraint` use the snake_case database names, with the schema outside `public`.
- `ExecuteUpdate` and raw SQL on an `IAuditable` type set the audit columns themselves.
- The schema changes only through a migration, added with the AppHost stopped:
  `dotnet dotnet-ef migrations add <Name> --project src/FocusTemplate.Data --startup-project src/admin/FocusTemplate.Admin.Api --context AppDbContext`,
  for a feature with its data project as both and its writable context.
- The spine's migrations and its permission seed are the product's, in `FocusTemplate.Data`, and what migrates the
  spine's context calls `UseProductMigrations()`. A shared model ships its migrations unless its data differs per
  product.
- A design-time factory takes its connection and the seed flag from `DesignTime`, and sits in the startup project or
  the context's project, where EF looks for it. The AppHost adds its migrations resource with `AddMigrations(…)`,
  a feature's followed by `.WaitForCompletion(migrations)`.
- The dev seed implements both the sync and the async delegate, and each part checks for its own rows before inserting.
- No migration creates or grants to `focusdb_reader`.

Background: [docs/database.md](../../docs/database.md).
