import "@typespec/http";
import "@typespec/openapi";
import "@spatialfocus/typespec-http-csharp-slim";

using Http;
using OpenAPI;
using SpatialFocus.Http;
using FocusTemplate.Primitives;

namespace FocusTemplate.Admin;

/** A weather forecast for one day. */
model WeatherForecastResponse {
  /** Identifier. */
  id: WeatherForecastId;

  /** Day the forecast applies to. */
  date: plainDate;

  /** Temperature in degrees Celsius. */
  temperatureC: int32;

  /** Free-text summary. */
  summary?: string;
}

@route("/weather-forecasts")
@tag("WeatherForecasts")
interface WeatherForecasts {
  /** Lists all forecasts, ordered by date. */
  @get
  list(): WeatherForecastResponse[];

  /** Gets one forecast by id. */
  @get
  get(@path id: WeatherForecastId): WeatherForecastResponse | Problem<404>;
}