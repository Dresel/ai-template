import "@typespec/http";
import "@typespec/openapi";
import "@spatialfocus/typespec-http-csharp-slim";

using Http;
using OpenAPI;
using SpatialFocus.Http;
using FocusTemplate.Primitives;

namespace FocusTemplate.Weather;

/** A weather station with its owner. */
model StationResponse {
  /** Identifier. */
  id: StationId;

  /** Short code, unique among the stations of one owner. */
  code: string;

  /** Display name. */
  name: string;

  /** The user who owns the station. */
  ownerId: UserId;

  /** The owner's display name. */
  ownerName: string;
}

/** A new name for a station. */
model StationRenameRequest {
  /** Display name. */
  @minLength(1)
  @maxLength(200)
  name: string;
}

@route("/stations")
@tag("Stations")
interface Stations {
  /** Lists the stations by code, each with its owner. */
  @get
  list(): StationResponse[];

  // No @requiresPermission: who may rename depends on the station, so the handler decides and answers 403
  /** Renames a station. Its owner may, and so may whoever holds Weather.ManageStations. */
  @route("/{id}:rename")
  @post
  rename(
    @path id: StationId,
    @body body: StationRenameRequest,
  ): StationResponse | ValidationProblem | Problem<403> | Problem<404>;
}