using Riok.Mapperly.Abstractions;
using SpatialFocus.Admin.UserManagement.Shared;
using SpatialFocus.Web.Foundation.Forms;

namespace SpatialFocus.Admin.UserManagement.Web.Groups;

[Mapper]
[UseStaticMapper(typeof(FormMappings))]
internal static partial class Mapper
{
	public static partial GroupRequest ToContract(this Form form);

	[MapperRequiredMapping(RequiredMappingStrategy.Target)]
	public static partial Form ToForm(this GroupResponse group);
}