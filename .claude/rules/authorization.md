---
paths:
  - "src/admin/user-management/*.Api/{Authorization,Provisioning}/**"
  - "src/**/spec/**/permissions.tsp"
  - "src/*.Data/Entities/{Group,GroupMember,GroupPermission,PermissionDefinition,User,UserActivity}*.cs"
  - "src/*.Web.Foundation/Authorization/**"
---
# Authorization

- A permission is a member of the slice's `@permissions` enum in the slice's `permissions.tsp`
  (`src/admin/spec/<slice>/`, user management's `src/admin/user-management/spec/`, a feature's
  `src/features/<name>/spec/`), which the product's `src/admin/spec/primitives.tsp` reaches, so it joins
  `AdminPermissions`. An operation or interface requires it with
  `@requiresPermission(Permissions.<Slice>.<Name>)`.
- A new permission is a migration in `FocusTemplate.Data` (`dotnet ef migrations add`), and so is renaming or removing
  one, since groups grant it by name.
- A page requires a permission with `@attribute [RequiresPermission(<Slice>Permissions.Names.<Name>)]`, a part of a page
  with `<PermissionView Permission="…">`, code with `CurrentUser.Has(…)` or `user.Has(…)`.
- The client only hides, the API decides.
- Permissions are granted to groups only. Writes go through the navigations (`group.Members.Add(user)`) after a filtered
  `Include` of what they touch.
- A managed group cannot be deleted or have its grants changed, and the last active administrator can neither leave
  `Administrators` nor be deactivated.
- A handler that changes a user's permissions (members, grants, deleting a group, activation) closes the user's open
  streams with `SignalBackplane.CloseStreamsFor` after `SaveChangesAsync`.

Background: [docs/authorization.md](../../docs/authorization.md).