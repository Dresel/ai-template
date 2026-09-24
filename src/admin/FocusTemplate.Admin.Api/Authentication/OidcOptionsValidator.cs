using Microsoft.Extensions.Options;

namespace FocusTemplate.Admin.Api.Authentication;

[OptionsValidator]
public sealed partial class OidcOptionsValidator : IValidateOptions<OidcOptions>;