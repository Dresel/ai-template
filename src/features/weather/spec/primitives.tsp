import "@spatialfocus/typespec-http-csharp-slim";

// The spine's ids and its permission scalar, which the feature's permissions take their values from
import "../../../spec/primitives.tsp";
import "./permissions.tsp";

using SpatialFocus.Http;

// The typed ids of the weather feature, with its permissions. SpatialFocus.Weather.Primitives writes them into the
// feature's own assembly, and a product's primitives.tsp imports this file for its permission list.
namespace SpatialFocus.Weather.Primitives;

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