using FocusTemplate.Admin.Shared;

namespace FocusTemplate.Admin.Client.DemoProfiles;

// Exhaustive over each union, so a status the spec adds breaks the build here
public static class DemoProfilesOutcomes
{
	extension(Task<DemoProfilesCheckCodeResult> call)
	{
		public Task<ApiOutcome<DemoCodeResponse>> ToOutcome() =>
			OutcomeSupport.CatchAsync(
				call,
				ApiOutcome<DemoCodeResponse> (result) => result switch
				{
					DemoCodeResponse code => code,
					ValidationProblem invalid => new ApiFailure(400, OutcomeSupport.ProblemOf(invalid.Problem)),
				});

		public Task<ApiOutcome<DemoCodeResponse, TProblem>> ToOutcome<TProblem>() =>
			OutcomeSupport.CatchAsync(
				call,
				ApiOutcome<DemoCodeResponse, TProblem> (result) => result.Value is TProblem problem
					? problem
					: result switch
					{
						DemoCodeResponse code => code,
						ValidationProblem invalid => new ApiFailure(400, OutcomeSupport.ProblemOf(invalid.Problem)),
					});
	}

	extension(Task<DemoProfilesCreateResult> call)
	{
		public Task<ApiOutcome<DemoProfileResponse>> ToOutcome() =>
			OutcomeSupport.CatchAsync(
				call,
				ApiOutcome<DemoProfileResponse> (result) => result switch
				{
					DemoProfileResponse profile => profile,
					ValidationProblem invalid => new ApiFailure(400, OutcomeSupport.ProblemOf(invalid.Problem)),
				});

		public Task<ApiOutcome<DemoProfileResponse, TProblem>> ToOutcome<TProblem>() =>
			OutcomeSupport.CatchAsync(
				call,
				ApiOutcome<DemoProfileResponse, TProblem> (result) => result.Value is TProblem problem
					? problem
					: result switch
					{
						DemoProfileResponse profile => profile,
						ValidationProblem invalid => new ApiFailure(400, OutcomeSupport.ProblemOf(invalid.Problem)),
					});
	}
}