import "@spatialfocus/typespec-http-csharp-slim";

// The permission sets the Admin product includes: the spine's with user management's, and each feature's.
// FocusTemplate.Admin.Primitives compiles this file into AdminPermissions, whose All lists every permission imported here.
import "../../spec/primitives.tsp";
import "../../features/weather/spec/primitives.tsp";