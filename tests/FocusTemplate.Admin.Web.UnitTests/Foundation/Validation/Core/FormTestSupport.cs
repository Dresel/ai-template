using FocusTemplate.Admin.Shared;
using FocusTemplate.Admin.Web.Foundation.Validation.Core;

namespace FocusTemplate.Admin.Web.UnitTests.Foundation.Validation.Core;

// What the form tests share: the codes a field shows, and the server's validation problem
internal static class FormTestSupport
{
	public static IEnumerable<string?> Codes(IEnumerable<FieldMessage> messages) =>
		messages.Select(message => message.Code);

	public static ValidationProblemDetails
		Problem(params (string Key, string Code, ViolationSeverity Severity)[] violations) =>
		new(
			violations.Where(violation => violation.Severity == ViolationSeverity.Error)
				.GroupBy(violation => violation.Key, StringComparer.Ordinal)
				.ToDictionary(
					group => group.Key,
					IReadOnlyList<string> (group) => [.. group.Select(violation => violation.Code),],
					StringComparer.Ordinal),
			[
				.. violations.Select(violation => new Violation(
					violation.Key,
					violation.Code,
					violation.Severity,
					violation.Code)),
			]);
}