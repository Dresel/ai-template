import "@typespec/http";
import "@typespec/openapi";
import "@spatialfocus/typespec-http-csharp-slim";

using Http;
using OpenAPI;
using SpatialFocus.Http;
using FocusTemplate.Primitives;

namespace FocusTemplate.Admin;

/** A group in a list. */
model GroupSummaryResponse {
  /** Identifier. */
  id: GroupId;

  /** Name, unique. */
  name: string;

  /** What the group is for. */
  description?: string;

  /** Managed by the application: its permissions come with each release, and it cannot be deleted. */
  isManaged: boolean;

  /** How many users are members. */
  memberCount: int32;
}

/** A member of a group. */
model GroupMemberResponse {
  /** Identifier of the user. */
  id: UserId;

  /** Display name. */
  displayName: string;

  /** False once the user is deactivated. */
  isActive: boolean;
}

/** A group with its members and permissions. */
model GroupResponse {
  ...GroupSummaryResponse;

  /** The members, by display name. */
  members: GroupMemberResponse[];

  /** The permissions the group grants its members. */
  permissions: Permission[];
}

/** The name and description of a group. */
model GroupRequest {
  /** Name, unique. */
  @minLength(1)
  @maxLength(100)
  name: string;

  /** What the group is for. */
  @maxLength(500)
  description?: string;
}

@route("/groups")
@tag("Groups")
interface Groups {
  /** Every group, by name. */
  @requiresPermission(Permissions.UserManagement.ViewGroups)
  @get
  list(): GroupSummaryResponse[];

  /** A group with its members and permissions. */
  @requiresPermission(Permissions.UserManagement.ViewGroups)
  @route("/{id}")
  @get
  get(@path id: GroupId): GroupResponse | Problem<404>;

  /** Creates a group without members or permissions. */
  @requiresPermission(Permissions.UserManagement.ManageGroups)
  @post
  create(@body body: GroupRequest): GroupResponse | ValidationProblem | Problem<409>;

  /** Renames a group or changes its description, a managed group too. */
  @requiresPermission(Permissions.UserManagement.ManageGroups)
  @route("/{id}")
  @put
  update(@path id: GroupId, @body body: GroupRequest): GroupResponse | ValidationProblem | Problem<404 | 409>;

  /** Deletes a group, and its members lose what it granted. Not a managed group. A missing group answers 204 too, so a retry stays idempotent. */
  @requiresPermission(Permissions.UserManagement.ManageGroups)
  @route("/{id}")
  @delete
  delete(@path id: GroupId): NoContentResponse | Problem<409>;

  /** Sets every permission the group grants, replacing the ones before. Not for a managed group. */
  @requiresPermission(Permissions.UserManagement.ManageGroups)
  @route("/{id}/permissions")
  @put
  setPermissions(@path id: GroupId, @body permissions: Permission[]): NoContentResponse | Problem<404 | 409>;

  /** Adds a user to the group. A member already stays one. */
  @requiresPermission(Permissions.UserManagement.ManageGroups)
  @route("/{id}/members/{userId}")
  @put
  addMember(@path id: GroupId, @path userId: UserId): NoContentResponse | Problem<404>;

  /** Removes a user from the group. One who is no member answers 204 too. Not the last active member of the Administrators. */
  @requiresPermission(Permissions.UserManagement.ManageGroups)
  @route("/{id}/members/{userId}")
  @delete
  removeMember(@path id: GroupId, @path userId: UserId): NoContentResponse | Problem<409>;
}
