using FocusTemplate.Admin.Shared.UserManagement;
using FocusTemplate.Admin.Web.Foundation.Forms;
using Riok.Mapperly.Abstractions;

namespace FocusTemplate.Admin.Web.Features.UserManagement.Groups;

[Mapper]
[UseStaticMapper(typeof(FormMappings))]
internal static partial class Mapper
{
	public static partial GroupRequest ToContract(this Form form);

	[MapperRequiredMapping(RequiredMappingStrategy.Target)]
	public static partial Form ToForm(this GroupResponse group);
}