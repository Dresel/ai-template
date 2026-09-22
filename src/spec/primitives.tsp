import "@spatialfocus/typespec-http-csharp-slim";

using SpatialFocus.Http;

/** Identifiers shared by the verticals of FocusTemplate. */
namespace FocusTemplate.Primitives;

/** Identifier of a weather forecast. */
@friendlyName("WeatherForecastId")
@typedId
scalar WeatherForecastId extends int32;