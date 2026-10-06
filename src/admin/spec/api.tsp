import "@typespec/http";
import "@typespec/openapi";
import "@spatialfocus/typespec-http-csharp-slim";

// The typed ids and permissions every vertical shares, generated once by FocusTemplate.Primitives and referenced by
// the slices. The paging query is spread into the list requests and generates nothing of its own.
import "../../spec/primitives.tsp";
import "../../spec/paging.tsp";

// One folder per feature slice next to this entry file, each contributing to the FocusTemplate.Admin namespace.
import "./demo-profiles/demo-profiles.tsp";
import "./user-management/groups.tsp";
import "./user-management/users.tsp";
import "./weather-forecasts/weather-forecasts.tsp";

using Http;
using OpenAPI;

// Every endpoint needs a signed-in user, and an operation names what else it requires with @requiresPermission
/** Internal API of the Admin vertical. Reached only through the BFF, never by a browser directly. */
@service(#{ title: "FocusTemplate Admin API" })
@info(#{ version: "0.1.0" })
@useAuth(BearerAuth)
namespace FocusTemplate.Admin;