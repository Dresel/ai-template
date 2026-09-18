import "@typespec/http";
import "@typespec/openapi";
import "@spatialfocus/typespec-http-csharp-slim";

// One file per feature slice next to this entry file; each contributes to the FocusTemplate.Admin namespace.
import "./WeatherForecasts.tsp";

using Http;
using OpenAPI;

/** Internal API of the Admin vertical. Reached only through the BFF, never by a browser directly. */
@service(#{ title: "FocusTemplate Admin API" })
@info(#{ version: "0.1.0" })
namespace FocusTemplate.Admin;