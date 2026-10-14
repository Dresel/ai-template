import "@typespec/http";
import "@typespec/openapi";
import "@spatialfocus/typespec-http-csharp-slim";

// The typed ids and permissions from the spine, which imports this folder's permissions.tsp, and the paging query the
// user search spreads
import "../../../spec/primitives.tsp";
import "../../../spec/paging.tsp";

import "./users.tsp";
import "./groups.tsp";

using Http;
using OpenAPI;

// A service of its own, so its projects generate their own contracts, clients and endpoints, which every Admin host maps
// into its API. Every endpoint needs a signed-in user, and an operation names what else it requires with
// @requiresPermission.
/** User management: the users who signed in, the groups and the permissions they grant. Reached through the BFF of the host's Admin API. */
@service(#{ title: "FocusTemplate User Management API" })
@info(#{ version: "0.1.0" })
@useAuth(BearerAuth)
namespace SpatialFocus.Admin.UserManagement;