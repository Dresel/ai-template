import "@spatialfocus/typespec-http-csharp-slim";

using SpatialFocus.Http;

// The spec of the shared spine: the typed ids every vertical uses, and nothing else. No @service on purpose:
// FocusTemplate.Primitives compiles this file alone with output-type: primitives into one Vogen struct per id, and
// each vertical's api.tsp imports it to reference the ids by this namespace, which is also their C# namespace.
// @friendlyName keeps the OpenAPI schema names bare: a type outside the service namespace is otherwise published as
// FocusTemplate.Primitives.<Name>. The C# name and namespace come from the scalar itself and do not change.
/** Identifiers shared by the verticals of FocusTemplate. */
namespace FocusTemplate.Primitives;

/** Identifier of a user, the owner of stations and the author of audited changes. */
@friendlyName("UserId")
@typedId
scalar UserId extends uuid;

/** Identifier of a weather station. */
@friendlyName("StationId")
@typedId
scalar StationId extends uuid;

/** Identifier of an observation, one measurement of a station. */
@friendlyName("ObservationId")
@typedId
scalar ObservationId extends int64;

/** Identifier of a weather forecast. */
@friendlyName("WeatherForecastId")
@typedId
scalar WeatherForecastId extends int32;

/** Identifier of an alert raised for a station. */
@friendlyName("AlertId")
@typedId
scalar AlertId extends uuid;
