import "@spatialfocus/typespec-http-csharp-slim";

import "../../../spec/primitives.tsp";

using SpatialFocus.Http;

// The permissions of the weather feature. SpatialFocus.Weather.Primitives writes their class beside the feature's ids,
// and a product that includes the feature lists them in its permission list.
namespace SpatialFocus.Weather.Primitives.Permissions;

/** What a user may do with the weather stations. */
@permissions(SpatialFocus.Primitives.Permission)
enum Weather {
  /** Rename any station, whoever owns it. */
  ManageStations,
}