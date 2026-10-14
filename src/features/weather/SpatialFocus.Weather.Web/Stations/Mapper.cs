using Riok.Mapperly.Abstractions;
using SpatialFocus.Weather.Shared;
using SpatialFocus.Web.Foundation.Forms;

namespace SpatialFocus.Weather.Web.Stations;

[Mapper]
[UseStaticMapper(typeof(FormMappings))]
internal static partial class Mapper
{
	public static partial StationRenameRequest ToContract(this Form form);

	[MapperIgnoreSource(nameof(StationResponse.Id))]
	[MapperIgnoreSource(nameof(StationResponse.Code))]
	[MapperIgnoreSource(nameof(StationResponse.OwnerId))]
	[MapperIgnoreSource(nameof(StationResponse.OwnerName))]
	public static partial Form ToForm(this StationResponse station);
}