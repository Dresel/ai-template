---
paths:
  - "src/admin/*.Admin.Api/Authorization/**"
  - "src/spec/permissions/**"
  - "src/*.Data/Entities/{Group,GroupMember,GroupPermission,PermissionDefinition,User,UserActivity}*.cs"
  - "src/admin/*.Admin.Web/Infrastructure/Authorization/**"
---
# Authorization

- A permission is a member of the slice's `@permissions` enum in `src/spec/permissions/<slice>.tsp`. An operation or
  interface requires it with `@requiresPermission(Permissions.<Slice>.<Name>)`.
- A new permission is a migration (`dotnet ef migrations add`), and so is renaming or removing one, since groups grant
  it by name.
- A page requires a permission with `@attribute [RequiresPermission(<Slice>Permissions.Names.<Name>)]`, a part of a page
  with `<PermissionView Permission="…">`, code with `CurrentUser.Has(…)` or `user.Has(…)`.
- The client only hides, the API decides.
- Permissions are granted to groups only. Writes go through the navigations (`group.Members.Add(user)`) after a filtered
  `Include` of what they touch.
- A managed group cannot be deleted or have its grants changed, and the last active administrator can neither leave
  `Administrators` nor be deactivated.

Background: [docs/authorization.md](../../docs/authorization.md).