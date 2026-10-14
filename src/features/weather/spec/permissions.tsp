import "@spatialfocus/typespec-http-csharp-slim";

using SpatialFocus.Http;

// The permissions of the weather feature. The feature's primitives.tsp imports this file, so SpatialFocus.Primitives
// writes them beside the host's, and the database seeds its permissions table from the Permission.All they make up.
namespace SpatialFocus.Primitives.Permissions;

/** What a user may do with the weather stations. */
@permissions(SpatialFocus.Primitives.Permission)
enum Weather {
  /** Rename any station, whoever owns it. */
  ManageStations,
}