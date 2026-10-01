import "@spatialfocus/typespec-http-csharp-slim";

using SpatialFocus.Http;

// The permissions of the user management slice. Beside the typed ids, outside any service: FocusTemplate.Primitives
// writes them, and the database seeds its permissions table from the Permission.All they make up.
namespace FocusTemplate.Primitives.Permissions;

/** What a user may do in the user management. */
@permissions(FocusTemplate.Primitives.Permission)
enum UserManagement {
  /** See the users, their groups and their permissions. */
  ViewUsers,

  /** Deactivate and reactivate users. */
  ManageUsers,

  /** See the groups, their members and their permissions. */
  ViewGroups,

  /** Create, rename and delete groups, set their permissions and members. Whoever holds it can grant themselves anything. */
  ManageGroups,
}
