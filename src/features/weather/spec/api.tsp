import "@typespec/http";
import "@typespec/openapi";
import "@spatialfocus/typespec-http-csharp-slim";

// The feature's typed ids and permissions, which bring the spine's along
import "./primitives.tsp";

import "./weather-forecasts.tsp";
import "./stations.tsp";

using Http;
using OpenAPI;

// A service of its own, so the feature's projects generate their own contracts, clients and endpoints, which a host maps
// into its Admin API. Every endpoint needs a signed-in user, as in the host.
/** The weather feature: forecasts and the stations they come from. Reached through the BFF of the host's Admin API. */
@service(#{ title: "FocusTemplate Weather API" })
@info(#{ version: "0.1.0" })
@useAuth(BearerAuth)
namespace SpatialFocus.Weather;