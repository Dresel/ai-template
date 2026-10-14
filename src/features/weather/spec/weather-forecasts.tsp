import "@typespec/http";
import "@typespec/openapi";
import "@spatialfocus/typespec-http-csharp-slim";

using Http;
using OpenAPI;
using SpatialFocus.Http;
using SpatialFocus.Primitives;

namespace SpatialFocus.Weather;

// One value for the model, the list and the single forecast, so the examples cannot drift apart
const mildDay = #{ id: 1, date: plainDate.fromISO("2026-10-14"), temperatureC: 14, summary: "Mild" };

/** A weather forecast for one day. */
@example(mildDay, #{ title: "A mild day" })
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
  @opExample(
    #{
      returnType: #[
        mildDay,
        #{ id: 2, date: plainDate.fromISO("2026-10-15"), temperatureC: 9, summary: "Cool" }
      ],
    },
    #{ title: "Two days" }
  )
  @get
  list(): WeatherForecastResponse[];

  /** Gets one forecast by id. */
  @opExample(#{ parameters: #{ id: 1 }, returnType: mildDay }, #{ title: "An existing forecast" })
  @get
  get(@path id: WeatherForecastId): WeatherForecastResponse | Problem<404>;
}