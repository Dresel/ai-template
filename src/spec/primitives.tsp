import "@spatialfocus/typespec-http-csharp-slim";

// The permissions of user management, which every Admin host includes. Its typed ids are the spine's own, declared below.
import "../admin/user-management/spec/permissions.tsp";

// Each feature the host includes, with its typed ids and its permissions
import "../features/weather/spec/primitives.tsp";

using SpatialFocus.Http;

// The spec of the shared spine: the typed ids and the permissions every vertical uses, and nothing else. No @service on purpose:
// FocusTemplate.Primitives compiles this file alone with output-type: primitives into one Vogen struct per id, and
// each vertical's api.tsp imports it to reference the ids by this namespace, which is also their C# namespace.
// @friendlyName gives the OpenAPI schemas the namespace's last segment as prefix, Primitives.<Name>. A type outside the
// service namespace is otherwise published fully qualified. The C# name and namespace come from the scalar itself and do
// not change.
/** Identifiers shared by the verticals of FocusTemplate. */
namespace FocusTemplate.Primitives;

/** Identifier of a user, the owner of stations and the author of audited changes. */
@friendlyName("Primitives.UserId")
@typedId
scalar UserId extends uuid;

/** Identifier of a group of users, the unit permissions are granted to. */
@friendlyName("Primitives.GroupId")
@typedId
scalar GroupId extends uuid;

/** A permission, named after its feature slice and action, such as UserManagement.ManageGroups. Admins grant permissions to groups. */
@friendlyName("Primitives.Permission")
@typedId
scalar Permission extends string;