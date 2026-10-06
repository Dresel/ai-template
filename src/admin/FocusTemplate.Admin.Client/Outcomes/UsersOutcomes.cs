using FocusTemplate.Admin.Shared;

namespace FocusTemplate.Admin.Client.Users;

// Exhaustive over each union, so a status the spec adds breaks the build here
public static class UsersOutcomes
{
	extension(Task<UsersActivateResult> call)
	{
		public Task<ApiOutcome<Success>> ToOutcome() =>
			OutcomeSupport.CatchAsync(
				call,
				ApiOutcome<Success> (result) => result switch
				{
					Success success => success,
					NotFoundProblem notFound => new ApiFailure(404, notFound.Problem),
				});

		public Task<ApiOutcome<Success, TProblem>> ToOutcome<TProblem>() =>
			OutcomeSupport.CatchAsync(
				call,
				ApiOutcome<Success, TProblem> (result) => result.Value is TProblem problem
					? problem
					: result switch
					{
						Success success => success,
						NotFoundProblem notFound => new ApiFailure(404, notFound.Problem),
					});
	}

	extension(Task<UsersDeactivateResult> call)
	{
		public Task<ApiOutcome<Success>> ToOutcome() =>
			OutcomeSupport.CatchAsync(
				call,
				ApiOutcome<Success> (result) => result switch
				{
					Success success => success,
					NotFoundProblem notFound => new ApiFailure(404, notFound.Problem),
					ConflictProblem conflict => new ApiFailure(409, conflict.Problem),
				});

		public Task<ApiOutcome<Success, TProblem>> ToOutcome<TProblem>() =>
			OutcomeSupport.CatchAsync(
				call,
				ApiOutcome<Success, TProblem> (result) => result.Value is TProblem problem
					? problem
					: result switch
					{
						Success success => success,
						NotFoundProblem notFound => new ApiFailure(404, notFound.Problem),
						ConflictProblem conflict => new ApiFailure(409, conflict.Problem),
					});
	}

	extension(Task<UsersGetResult> call)
	{
		public Task<ApiOutcome<UserResponse>> ToOutcome() =>
			OutcomeSupport.CatchAsync(
				call,
				ApiOutcome<UserResponse> (result) => result switch
				{
					UserResponse user => user,
					NotFoundProblem notFound => new ApiFailure(404, notFound.Problem),
				});

		public Task<ApiOutcome<UserResponse, TProblem>> ToOutcome<TProblem>() =>
			OutcomeSupport.CatchAsync(
				call,
				ApiOutcome<UserResponse, TProblem> (result) => result.Value is TProblem problem
					? problem
					: result switch
					{
						UserResponse user => user,
						NotFoundProblem notFound => new ApiFailure(404, notFound.Problem),
					});
	}

	extension(Task<UsersSearchResult> call)
	{
		public Task<ApiOutcome<UserPageResponse>> ToOutcome() =>
			OutcomeSupport.CatchAsync(
				call,
				ApiOutcome<UserPageResponse> (result) => result switch
				{
					UserPageResponse page => page,
					ValidationProblem invalid => new ApiFailure(400, OutcomeSupport.ProblemOf(invalid.Problem)),
				});

		public Task<ApiOutcome<UserPageResponse, TProblem>> ToOutcome<TProblem>() =>
			OutcomeSupport.CatchAsync(
				call,
				ApiOutcome<UserPageResponse, TProblem> (result) => result.Value is TProblem problem
					? problem
					: result switch
					{
						UserPageResponse page => page,
						ValidationProblem invalid => new ApiFailure(400, OutcomeSupport.ProblemOf(invalid.Problem)),
					});
	}
}