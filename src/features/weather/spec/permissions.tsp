import "@spatialfocus/typespec-http-csharp-slim";

using SpatialFocus.Http;

// The permissions of the weather feature. The host's primitives.tsp imports this file, so FocusTemplate.Primitives
// writes them beside the host's, and the database seeds its permissions table from the Permission.All they make up.
namespace FocusTemplate.Primitives.Permissions;

/** What a user may do with the weather stations. */
@permissions(FocusTemplate.Primitives.Permission)
enum Weather {
  /** Rename any station, whoever owns it. */
  ManageStations,
}