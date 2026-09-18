import "@typespec/http";
import "@typespec/openapi";
import "@spatialfocus/typespec-http-csharp-slim";

// One file per feature slice next to this entry file; each contributes to the FocusTemplate.Public namespace.
import "./WeatherForecasts.tsp";

using Http;
using OpenAPI;

/** The deliberately exposed API of the Public vertical, consumed by the native mobile client. */
@service(#{ title: "FocusTemplate Public API" })
@info(#{ version: "0.1.0" })
namespace FocusTemplate.Public;