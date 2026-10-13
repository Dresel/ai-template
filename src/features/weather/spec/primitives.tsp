import "@spatialfocus/typespec-http-csharp-slim";

import "./permissions.tsp";

using SpatialFocus.Http;

// The typed ids of the weather feature, with its permissions. The host's primitives.tsp imports this file, so
// FocusTemplate.Primitives writes them beside the host's own.
namespace FocusTemplate.Primitives;

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