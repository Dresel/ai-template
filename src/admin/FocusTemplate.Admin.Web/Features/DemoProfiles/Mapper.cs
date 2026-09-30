using FocusTemplate.Admin.Shared;
using FocusTemplate.Admin.Web.Foundation.Forms;
using Riok.Mapperly.Abstractions;

namespace FocusTemplate.Admin.Web.Features.DemoProfiles;

[Mapper]
[UseStaticMapper(typeof(FormMappings))]
internal static partial class Mapper
{
	public static IReadOnlyDictionary<string, string> Renames { get; } =
		new Dictionary<string, string> { [AdminPaths.DemoProfileRequest.Name] = nameof(Form.DisplayName), };

	[MapProperty(nameof(Form.DisplayName), nameof(DemoProfileRequest.Name))]
	[MapProperty(nameof(Form.Age), nameof(DemoProfileRequest.Age), Use = nameof(AgeSent))]
	[MapperIgnoreSource(nameof(Form.ClientRules))]
	public static partial DemoProfileRequest ToContract(this Form form);

	public static partial AcceptedProfile ToAcceptedProfile(this DemoProfileResponse response);

	// Only for the demo's switch: an empty age goes out as 0, which the contract's minimum of 18 rejects
	[UserMapping(Default = false)]
	private static int AgeSent(int? age) => age ?? 0;

	// An empty row is an empty tag, which the contract rejects at its index
	private static string ToTag(TagRow row) => row.Value ?? string.Empty;
}