using FocusTemplate.Primitives;
using Vogen;

namespace FocusTemplate.Weather.Data;

[EfCoreConverter<StationId>]
[EfCoreConverter<ObservationId>]
[EfCoreConverter<WeatherForecastId>]
[EfCoreConverter<AlertId>]
internal sealed partial class WeatherEfCoreConverters;