using FocusTemplate.Admin.Web.Foundation.Forms;
using FocusTemplate.Weather.Shared;
using Riok.Mapperly.Abstractions;

namespace FocusTemplate.Weather.Web.Stations;

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