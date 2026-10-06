import "@typespec/http";
import "@typespec/openapi";
import "@spatialfocus/typespec-http-csharp-slim";

using Http;
using OpenAPI;
using SpatialFocus.Http;
using FocusTemplate.Paging;
using FocusTemplate.Primitives;

namespace FocusTemplate.Admin;

/** The signed-in user and what they may do. */
model CurrentUserResponse {
  /** Identifier, the subject the identity provider issued. */
  id: UserId;

  /** Display name. */
  displayName: string;

  /** Every permission the user's groups grant, so the UI can hide what the user may not do. */
  permissions: Permission[];
}

/** A user in a list. */
model UserSummaryResponse {
  /** Identifier, the subject the identity provider issued. */
  id: UserId;

  /** Display name, as the identity provider last reported it. */
  displayName: string;

  /** E-mail address, as the identity provider last reported it. */
  email?: string;

  /** False once deactivated: the user then holds no permission, whatever their token says. */
  isActive: boolean;

  /** Last request the user made, to a quarter of an hour. */
  lastSeenAt: utcDateTime;
}

/** A group a user is a member of. */
model GroupReferenceResponse {
  /** Identifier. */
  id: GroupId;

  /** Name. */
  name: string;
}

/** A user with their groups and the permissions those grant. */
model UserResponse {
  ...UserSummaryResponse;

  /** First request the user made. */
  firstSeenAt: utcDateTime;

  /** The groups the user is a member of, by name. */
  groups: GroupReferenceResponse[];

  /** Every permission the user's groups grant. */
  permissions: Permission[];
}

/** What a user list is ordered by. */
enum UserSort {
  /** By display name, the default. */
  displayName,

  /** By the last request the user made. */
  lastSeenAt,

  /** By e-mail address. Users without one come last in either direction. */
  email,
}

/** Which users to list, in which order, and which page of them. */
model UserSearchRequest {
  /** Part of the display name or e-mail address. */
  @maxLength(100)
  search?: string;

  /** Order of the list, by display name unless given. */
  sort?: UserSort;

  /** From last to first instead. */
  descending?: boolean;

  ...PageQuery;
}

/** A page of users, and how many match in all. */
model UserPageResponse {
  /** The users of this page. */
  items: UserSummaryResponse[];

  /** How many users match in all. */
  total: int32;
}

// The permission per operation, not on the interface: the signed-in user's own record needs none
@route("/users")
@tag("Users")
interface Users {
  /** The signed-in user and their permissions. */
  @route("/me")
  @get
  me(): CurrentUserResponse;

  /** Searches the users who have signed in at least once. */
  @requiresPermission(Permissions.UserManagement.ViewUsers)
  @httpQuery
  @post
  search(@body body: UserSearchRequest): UserPageResponse | ValidationProblem;

  /** A user, their groups and their permissions. */
  @requiresPermission(Permissions.UserManagement.ViewUsers)
  @route("/{id}")
  @get
  get(@path id: UserId): UserResponse | Problem<404>;

  /** Deactivates a user: from their next request on they hold no permission. Neither the caller nor the last active administrator. */
  @requiresPermission(Permissions.UserManagement.ManageUsers)
  @route("/{id}:deactivate")
  @post
  deactivate(@path id: UserId): NoContentResponse | Problem<404 | 409>;

  /** Reactivates a deactivated user. */
  @requiresPermission(Permissions.UserManagement.ManageUsers)
  @route("/{id}:activate")
  @post
  activate(@path id: UserId): NoContentResponse | Problem<404>;
}
