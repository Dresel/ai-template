using FocusTemplate.Admin.Shared;

namespace FocusTemplate.Admin.Client;

// The client throws HttpRequestException for a status the contract does not model and when no answer arrives, so an
// outcome takes it as a failure. Whatever else a call throws stays an exception
internal static class OutcomeSupport
{
	public static async Task<ApiOutcome<T>> CatchAsync<TResult, T>(Task<TResult> call, Func<TResult, ApiOutcome<T>> map)
	{
		try
		{
			return map(await call.ConfigureAwait(false));
		}
		catch (HttpRequestException exception)
		{
			return new ApiFailure((int?)exception.StatusCode, null);
		}
	}

	public static async Task<ApiOutcome<T, TProblem>> CatchAsync<TResult, T, TProblem>(
		Task<TResult> call,
		Func<TResult, ApiOutcome<T, TProblem>> map)
	{
		try
		{
			return map(await call.ConfigureAwait(false));
		}
		catch (HttpRequestException exception)
		{
			return new ApiFailure((int?)exception.StatusCode, null);
		}
	}

	// A validation problem left to the failure path: its field errors have no place there
	public static ProblemDetails ProblemOf(ValidationProblemDetails problem) =>
		new(problem.Type, problem.Title, problem.Status, problem.Detail, problem.Instance);
}