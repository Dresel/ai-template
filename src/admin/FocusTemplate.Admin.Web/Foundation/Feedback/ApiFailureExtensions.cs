using FocusTemplate.Admin.Client;

namespace FocusTemplate.Admin.Web.Foundation.Feedback;

public static class ApiFailureExtensions
{
	extension(ApiFailure failure)
	{
		// The server's own words where it sent any. No status means no answer arrived: the server was unreachable, too
		// slow, or the resilience handler stopped asking after repeated failures
		public string Message =>
			failure.Problem?.Detail ?? failure.Problem?.Title ?? (failure.Status is { } status
				? $"The server answered {status}."
				: "The server is not answering right now.");
	}
}