using FluentValidation;
using FocusTemplate.Admin.Web.Foundation.Validation.Core;
using Radzen;

namespace FocusTemplate.Admin.Web.Foundation.Forms;

internal static class Severities
{
	public static AlertStyle Alert(Severity severity) =>
		severity switch
		{
			Severity.Error => AlertStyle.Danger,
			Severity.Warning => AlertStyle.Warning,
			_ => AlertStyle.Info,
		};

	public static string Icon(Severity severity) =>
		severity switch
		{
			Severity.Error => "cancel",
			Severity.Warning => "warning",
			_ => "info",
		};

	public static IReadOnlyList<FieldMessage> MostSevereFirst(IEnumerable<FieldMessage> messages) =>
	[
		.. messages.OrderBy(message => message.Severity),
	];

	public static string Name(Severity severity) =>
		severity switch
		{
			Severity.Error => "error",
			Severity.Warning => "warning",
			_ => "info",
		};
}