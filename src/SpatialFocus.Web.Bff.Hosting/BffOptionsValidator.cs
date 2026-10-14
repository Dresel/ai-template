using Microsoft.Extensions.Options;

namespace SpatialFocus.Web.Bff.Hosting;

[OptionsValidator]
public sealed partial class BffOptionsValidator : IValidateOptions<BffOptions>;