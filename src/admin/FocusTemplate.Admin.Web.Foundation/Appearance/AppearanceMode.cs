using System.Text.Json.Serialization;

namespace FocusTemplate.Admin.Web.Foundation.Appearance;

[JsonConverter(typeof(JsonStringEnumConverter<AppearanceMode>))]
public enum AppearanceMode
{
	[JsonStringEnumMemberName("system")]
	System,

	[JsonStringEnumMemberName("light")]
	Light,

	[JsonStringEnumMemberName("dark")]
	Dark,
}