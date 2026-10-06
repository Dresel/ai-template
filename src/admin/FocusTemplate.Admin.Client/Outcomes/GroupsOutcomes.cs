using FocusTemplate.Admin.Shared;

namespace FocusTemplate.Admin.Client.Groups;

// Exhaustive over each union, so a status the spec adds breaks the build here
public static class GroupsOutcomes
{
	// An operation that models no problem answers without a union, but fails the same way
	extension(Task<IReadOnlyList<GroupSummaryResponse>> call)
	{
		public Task<ApiOutcome<IReadOnlyList<GroupSummaryResponse>>> ToOutcome() =>
			OutcomeSupport.CatchAsync(call, ApiOutcome<IReadOnlyList<GroupSummaryResponse>> (groups) => groups);
	}

	extension(Task<GroupsAddMemberResult> call)
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

	extension(Task<GroupsCreateResult> call)
	{
		public Task<ApiOutcome<GroupResponse>> ToOutcome() =>
			OutcomeSupport.CatchAsync(
				call,
				ApiOutcome<GroupResponse> (result) => result switch
				{
					GroupResponse group => group,
					ValidationProblem invalid => new ApiFailure(400, OutcomeSupport.ProblemOf(invalid.Problem)),
					ConflictProblem conflict => new ApiFailure(409, conflict.Problem),
				});

		public Task<ApiOutcome<GroupResponse, TProblem>> ToOutcome<TProblem>() =>
			OutcomeSupport.CatchAsync(
				call,
				ApiOutcome<GroupResponse, TProblem> (result) => result.Value is TProblem problem
					? problem
					: result switch
					{
						GroupResponse group => group,
						ValidationProblem invalid => new ApiFailure(400, OutcomeSupport.ProblemOf(invalid.Problem)),
						ConflictProblem conflict => new ApiFailure(409, conflict.Problem),
					});
	}

	extension(Task<GroupsDeleteResult> call)
	{
		public Task<ApiOutcome<Success>> ToOutcome() =>
			OutcomeSupport.CatchAsync(
				call,
				ApiOutcome<Success> (result) => result switch
				{
					Success success => success,
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
						ConflictProblem conflict => new ApiFailure(409, conflict.Problem),
					});
	}

	extension(Task<GroupsGetResult> call)
	{
		public Task<ApiOutcome<GroupResponse>> ToOutcome() =>
			OutcomeSupport.CatchAsync(
				call,
				ApiOutcome<GroupResponse> (result) => result switch
				{
					GroupResponse group => group,
					NotFoundProblem notFound => new ApiFailure(404, notFound.Problem),
				});

		public Task<ApiOutcome<GroupResponse, TProblem>> ToOutcome<TProblem>() =>
			OutcomeSupport.CatchAsync(
				call,
				ApiOutcome<GroupResponse, TProblem> (result) => result.Value is TProblem problem
					? problem
					: result switch
					{
						GroupResponse group => group,
						NotFoundProblem notFound => new ApiFailure(404, notFound.Problem),
					});
	}

	extension(Task<GroupsRemoveMemberResult> call)
	{
		public Task<ApiOutcome<Success>> ToOutcome() =>
			OutcomeSupport.CatchAsync(
				call,
				ApiOutcome<Success> (result) => result switch
				{
					Success success => success,
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
						ConflictProblem conflict => new ApiFailure(409, conflict.Problem),
					});
	}

	extension(Task<GroupsSetPermissionsResult> call)
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

	extension(Task<GroupsUpdateResult> call)
	{
		public Task<ApiOutcome<GroupResponse>> ToOutcome() =>
			OutcomeSupport.CatchAsync(
				call,
				ApiOutcome<GroupResponse> (result) => result switch
				{
					GroupResponse group => group,
					ValidationProblem invalid => new ApiFailure(400, OutcomeSupport.ProblemOf(invalid.Problem)),
					NotFoundProblem notFound => new ApiFailure(404, notFound.Problem),
					ConflictProblem conflict => new ApiFailure(409, conflict.Problem),
				});

		public Task<ApiOutcome<GroupResponse, TProblem>> ToOutcome<TProblem>() =>
			OutcomeSupport.CatchAsync(
				call,
				ApiOutcome<GroupResponse, TProblem> (result) => result.Value is TProblem problem
					? problem
					: result switch
					{
						GroupResponse group => group,
						ValidationProblem invalid => new ApiFailure(400, OutcomeSupport.ProblemOf(invalid.Problem)),
						NotFoundProblem notFound => new ApiFailure(404, notFound.Problem),
						ConflictProblem conflict => new ApiFailure(409, conflict.Problem),
					});
	}
}