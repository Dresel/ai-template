using Microsoft.Extensions.Options;

namespace FocusTemplate.Web.Bff.Hosting;

[OptionsValidator]
public sealed partial class OidcOptionsValidator : IValidateOptions<OidcOptions>;