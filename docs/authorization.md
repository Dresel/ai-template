# Authorization

What to follow when changing these files: [the rule](../.claude/rules/authorization.md).

Keycloak says who the user is, the database says what they may do. No roles in the token, no ASP.NET Identity.

## Permissions

- **In the spec**: one enum per slice in `src/spec/permissions/<slice>.tsp`, in namespace
  `FocusTemplate.Primitives.Permissions`, marked `@permissions(FocusTemplate.Primitives.Permission)`
  (`enum UserManagement { ViewUsers, ManageUsers, ViewGroups, ManageGroups }`).
- **Generated** into Primitives as `UserManagementPermissions.ViewUsers` (the `Permission` "UserManagement.ViewUsers"),
  beside `Permission` in `FocusTemplate.Primitives`, plus `Permission.All` and the constants in
  `UserManagementPermissions.Names` for attribute arguments.
- **Required** by an operation or interface with `@requiresPermission(Permissions.UserManagement.ViewUsers)`, qualified
  because the enum lives outside the service namespace. The generated endpoint carries it as
  `RequiresPermissionAttribute` metadata (generated into `Admin.Shared`) and answers 403 without it. An operation
  without one only needs a signed-in user.
- **Stored** by name: groups grant permissions by name, so renaming or removing one is a migration.
  `PermissionDefinition` is the table of the release's permissions, seeded by `HasData` from `Permission.All`, so a new
  permission is `dotnet ef migrations add`.

## One mechanism on both ends

No policies and no roles: the attribute yields a `PermissionRequirement`, and each side registers its own handler.

- **API**: `PermissionRequirementHandler` asks the scoped `UserPermissions`, one query per request over the groups of
  an active user, so a deactivated user holds nothing, whatever their token.
- **WASM**: `PermissionClaimsHandler` reads the `permission` claims (`BffClaimTypes.Permission`) of `/bff/user`, which
  the BFF adds from the API's `GET /users/me` with the session's token, so a page load costs one request. Without an
  answer from the API, `/bff/user` fails (see [authentication](authentication.md)) and never hands out a user without
  permissions.
- **Pages**: `@attribute [RequiresPermission(UserManagementPermissions.Names.ViewUsers)]`, which `AuthorizeRouteView`
  honors from .NET 11 (`RequiresPermissionTests`). `AuthorizeView` takes only a policy or roles, so a part of a page
  asks with `<PermissionView Permission="…">`, an `AuthorizeViewCore` handing over the same requirement
  (`PermissionViewTests`). Code that weighs several permissions, such as the menu, reads `user.Has(permission)`. A
  page's code asks the injected `CurrentUser` (`Has(permission)`, `Id`), which reads the state the router authorized
  the page with: `BffAuthenticationStateProvider` loads it once, and a page renders only after that.

The client only hides, the API decides.

## Model

- A `Group` grants permissions (`Group.Permissions`) and has users as members (`Group.Members`, `User.Groups`). Nothing
  is granted to a user directly.
- Both many-to-many relationships have a join entity class (`GroupPermission`, `GroupMember`), because a join entity EF
  makes up is no `IAuditable`, and who added a member or granted a permission is the audit trail that matters most.
  Writes go through the navigations (`group.Members.Add(user)`, after a filtered `Include` of what they touch), and EF
  adds or deletes the join row.
- A managed group (`IsManaged`) comes with the release: it cannot be deleted or have its grants changed. The one today
  is `Administrators` (`WellKnownGroups.Administrators`), seeded by the migration with every permission. The last
  active administrator can neither leave it nor be deactivated, so someone can always administer the application.

## Runtime

`UserProvisioningMiddleware` runs between authentication and authorization. It creates the token's user in `users` on
their first request, and at most once per 15 minutes and instance (remembered in `IMemoryCache`) writes
`user_activities.last_seen_at` and refreshes the display name and e-mail from the token. The activity goes through
`User.Activity` in the same `SaveChanges`. The activity table is no `IAuditable`, so the user's audit columns stay the
changes somebody made.

## First administrator

- **Run mode**: the dev seed makes `WellKnownUsers.Developer` a member of `Administrators`.
- **Production**: the Admin API image runs once as a job with `bootstrap-admin --subject <keycloak sub>`
  (`AdministratorBootstrap`). It is idempotent, works before the user's first sign-in, reactivates the user, and is the
  repair when `Administrators` lost its last active member.

## Tests

`UserManagementArrangements` arranges users, a group granting permissions to one user (`GrantAsync`) and the managed
groups (`AddAdministratorsAsync`, `AddManagedGroupAsync`), which the reset removes along with everything but the
permissions and the migration history. [Integration tests](integration-tests.md) describes when a test needs a user of
its own.