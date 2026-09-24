using Microsoft.Extensions.Options;

namespace FocusTemplate.Admin.Web.Bff;

[OptionsValidator]
public sealed partial class OidcOptionsValidator : IValidateOptions<OidcOptions>;