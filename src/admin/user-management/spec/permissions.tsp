import "@spatialfocus/typespec-http-csharp-slim";

using SpatialFocus.Http;

// The permissions of user management, outside its service. The spine's primitives.tsp imports this file, so
// SpatialFocus.Primitives writes them, and the database seeds its permissions table from Permission.All.
namespace SpatialFocus.Primitives.Permissions;

/** What a user may do in the user management. */
@permissions(SpatialFocus.Primitives.Permission)
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