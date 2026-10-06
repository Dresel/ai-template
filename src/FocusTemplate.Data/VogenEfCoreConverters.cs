using FocusTemplate.Primitives;
using Vogen;

namespace FocusTemplate.Data;

// Converters are generated here because FocusTemplate.Primitives cannot reference EF Core.
// AppDbContextBase.ConfigureConventions registers them via RegisterAllInVogenEfCoreConverters.
[EfCoreConverter<UserId>]
[EfCoreConverter<StationId>]
[EfCoreConverter<ObservationId>]
[EfCoreConverter<WeatherForecastId>]
[EfCoreConverter<AlertId>]
[EfCoreConverter<GroupId>]
[EfCoreConverter<Permission>]
internal sealed partial class VogenEfCoreConverters;