using SpatialFocus.Primitives;
using Vogen;

namespace SpatialFocus.Weather.Data;

[EfCoreConverter<StationId>]
[EfCoreConverter<ObservationId>]
[EfCoreConverter<WeatherForecastId>]
[EfCoreConverter<AlertId>]
internal sealed partial class WeatherEfCoreConverters;