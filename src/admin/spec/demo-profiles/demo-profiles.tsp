import "@typespec/http";
import "@typespec/openapi";
import "@spatialfocus/typespec-http-csharp-slim";

using Http;
using OpenAPI;
using SpatialFocus.Http;

namespace FocusTemplate.Admin;

// The validation demo behind the demo pages: every constraint kind the emitter turns into rules, plus the server-side
// rules of DemoProfileRequestValidator. Nothing is stored.

/** A profile's short code: three upper-case letters. */
@pattern("^[A-Z]{3}$", "Three upper-case letters.")
scalar DemoCode extends string;

/** A tag: one to twenty characters. */
@minLength(1)
@maxLength(20)
scalar DemoTag extends string;

/** A postal address. */
model DemoAddressRequest {
  /** Street and number. */
  @maxLength(100)
  street: string;

  /** Postal code: four digits. */
  @pattern("^[0-9]{4}$", "Four digits.")
  postalCode: string;

  /** City. */
  @maxLength(50)
  city: string;
}

/** A profile to validate. */
model DemoProfileRequest {
  /** Short code: three upper-case letters, not yet taken. */
  code: DemoCode;

  /** Display name. */
  @minLength(1)
  @maxLength(50)
  name: string;

  /** Nickname. */
  @maxLength(20)
  nickname?: string;

  /** Age in years. */
  @minValue(18)
  @maxValue(120)
  age: int32;

  /** Weight in kilograms, above zero. */
  @minValueExclusive(0)
  weightKg?: float64;

  /** Lowest comfortable temperature in degrees Celsius, at most the highest. */
  minTemperatureC?: int32;

  /** Highest comfortable temperature in degrees Celsius, at least the lowest. */
  maxTemperatureC?: int32;

  /** Postal address. */
  address: DemoAddressRequest;

  /** Tags, at most five. */
  @maxItems(5)
  tags?: DemoTag[];
}

/** Whether a profile holds a code. */
model DemoCodeResponse {
  /** The code as asked for. */
  code: string;

  /** True when a profile holds the code, so creating another one with it fails. */
  taken: boolean;
}

/** A profile as accepted. */
model DemoProfileResponse {
  /** Short code. */
  code: string;

  /** Display name. */
  name: string;
}

@route("/demo-profiles")
@tag("DemoProfiles")
interface DemoProfiles {
  /** Validates a profile and answers it back without storing it. */
  @post
  create(@body body: DemoProfileRequest): DemoProfileResponse | ValidationProblem;

  /** Tells whether a profile holds the code, for a form that checks it while the user types. Creating a profile checks
   * again, since another one can take the code in between. */
  @get
  @route("/codes/{code}")
  checkCode(@path code: DemoCode): DemoCodeResponse | ValidationProblem;
}