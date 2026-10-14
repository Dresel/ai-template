using Microsoft.Extensions.Options;

namespace SpatialFocus.Api.Hosting.Authentication;

[OptionsValidator]
public sealed partial class OidcOptionsValidator : IValidateOptions<OidcOptions>;