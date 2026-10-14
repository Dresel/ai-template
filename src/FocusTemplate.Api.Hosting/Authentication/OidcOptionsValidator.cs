using Microsoft.Extensions.Options;

namespace FocusTemplate.Api.Hosting.Authentication;

[OptionsValidator]
public sealed partial class OidcOptionsValidator : IValidateOptions<OidcOptions>;