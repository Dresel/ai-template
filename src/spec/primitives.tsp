import "@spatialfocus/typespec-http-csharp-slim";

// The permissions of the verticals, one file per feature slice
import "./permissions/user-management.tsp";

using SpatialFocus.Http;

// The spec of the shared spine: the typed ids and the permissions every vertical uses, and nothing else. No @service on purpose:
// FocusTemplate.Primitives compiles this file alone with output-type: primitives into one Vogen struct per id, and
// each vertical's api.tsp imports it to reference the ids by this namespace, which is also their C# namespace.
// @friendlyName gives the OpenAPI schemas the namespace's last segment as prefix, Primitives.<Name>, as a feature's
// models carry UserManagement.<Name>. A type outside the service namespace is otherwise published fully qualified. The
// C# name and namespace come from the scalar itself and do not change.
/** Identifiers shared by the verticals of FocusTemplate. */
namespace FocusTemplate.Primitives;

/** Identifier of a user, the owner of stations and the author of audited changes. */
@friendlyName("Primitives.UserId")
@typedId
scalar UserId extends uuid;

/** Identifier of a weather station. */
@friendlyName("Primitives.StationId")
@typedId
scalar StationId extends uuid;

/** Identifier of an observation, one measurement of a station. */
@friendlyName("Primitives.ObservationId")
@typedId
scalar ObservationId extends int64;

/** Identifier of a weather forecast. */
@friendlyName("Primitives.WeatherForecastId")
@typedId
scalar WeatherForecastId extends int32;

/** Identifier of an alert raised for a station. */
@friendlyName("Primitives.AlertId")
@typedId
scalar AlertId extends uuid;

/** Identifier of a group of users, the unit permissions are granted to. */
@friendlyName("Primitives.GroupId")
@typedId
scalar GroupId extends uuid;

/** A permission, named after its feature slice and action, such as UserManagement.ManageGroups. Admins grant permissions to groups. */
@friendlyName("Primitives.Permission")
@typedId
scalar Permission extends string;
