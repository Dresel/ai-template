using FocusTemplate.Admin.Client;

namespace FocusTemplate.Admin.Web.Foundation.Feedback;

public sealed class ApiFailureMessages(IApiFailureMessagesLocalizations localizations)
{
	// The server's own words where it sent any. No status means no answer arrived: the server was unreachable, too slow,
	// or the resilience handler stopped asking after repeated failures
	public string Of(ApiFailure failure) =>
		failure.Problem?.Detail ?? failure.Problem?.Title ?? (failure.Status is { } status
			? localizations.Answered(status)
			: localizations.NotAnswering);
}