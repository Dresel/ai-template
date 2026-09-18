import "@typespec/http";
import "@typespec/openapi";
import "@spatialfocus/typespec-http-csharp-slim";

using Http;
using OpenAPI;
using SpatialFocus.Http;

namespace FocusTemplate.Admin;

/** A weather forecast for one day. */
model WeatherForecastResponse {
  /** Identifier. */
  id: int32;

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
  get(@path id: int32): WeatherForecastResponse | Problem<404>;
}