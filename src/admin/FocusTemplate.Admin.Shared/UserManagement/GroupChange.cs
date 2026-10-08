using System.Text.Json.Serialization;

namespace FocusTemplate.Admin.Shared.UserManagement;

[JsonConverter(typeof(JsonStringEnumConverter<GroupChange>))]
public enum GroupChange
{
	[JsonStringEnumMemberName("created")]
	Created,

	[JsonStringEnumMemberName("updated")]
	Updated,

	[JsonStringEnumMemberName("deleted")]
	Deleted,
}