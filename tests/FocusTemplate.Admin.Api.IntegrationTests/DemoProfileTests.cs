using FocusTemplate.Admin.Client;
using FocusTemplate.Admin.Client.DemoProfiles;
using FocusTemplate.Admin.Shared;
using Xunit.Sdk;

namespace FocusTemplate.Admin.Api.IntegrationTests;

public sealed class DemoProfileTests(ApiFixture factory) : ApiTestBase(factory)
{
	private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

	private static DemoProfileRequest Valid => new("ABC", "Ada", 36, new DemoAddressRequest("Hohe Warte 38", "1190", "Vienna"));

	[Theory]
	[InlineData("TAK", true)]
	[InlineData("ABC", false)]
	public async Task ACheckTellsWhetherAProfileHoldsTheCode(string code, bool taken)
	{
		DemoProfilesCheckCodeResult result = await Client().CheckCodeAsync(code, Cancellation);

		Assert.Equal(new DemoCodeResponse(code, taken), result is DemoCodeResponse response ? response : null);
	}

	[Fact]
	public async Task ACodeTheServerKnowsIsTakenFailsThere()
	{
		ValidationProblemDetails problem = await CreateInvalidAsync(Valid with { Code = "TAK", });

		Violation taken = Assert.Single(problem.Violations);
		Assert.Equal(("code", "code.taken"), (taken.Key, taken.Code));
	}

	[Fact]
	public async Task AFieldCanFailSeveralRulesOfEverySeverity()
	{
		ValidationProblemDetails problem = await CreateInvalidAsync(Valid with { Nickname = "SHOUTING 12 SHOUTING 34", });

		Assert.Equal(
			[
				("maxLength", ViolationSeverity.Error),
				("nickname.digits", ViolationSeverity.Error),
				("nickname.lettersOnly", ViolationSeverity.Info),
				("nickname.shouting", ViolationSeverity.Warning),
				("nickname.spaces", ViolationSeverity.Error),
			],
			problem.Violations.Where(violation => violation.Key == "nickname")
				.Select(violation => (violation.Code, violation.Severity))
				.Order());
	}

	[Fact]
	public async Task ALanguageTheApiDoesNotSpeakIsAnsweredInEnglish()
	{
		ValidationProblemDetails problem = await CreateInvalidAsync(Valid with { Code = "abc", }, "fr");

		Assert.Equal(["Three upper-case letters.",], problem.Errors["code"]);
	}

	[Fact]
	public async Task ARuleOnTheWholeAddressIsKeyedToTheAddress()
	{
		ValidationProblemDetails problem = await CreateInvalidAsync(
			Valid with { Address = new DemoAddressRequest("Main Street 1", "1234", "Atlantis"), });

		Assert.Equal(["address",], problem.Errors.Keys);
	}

	[Fact]
	public async Task AValidProfileIsAnsweredBack()
	{
		DemoProfilesCreateResult result = await Client().CreateAsync(Valid, Cancellation);

		Assert.Equal(new DemoProfileResponse("ABC", "Ada"), result is DemoProfileResponse profile ? profile : null);
	}

	[Fact]
	public async Task CrossedTemperaturesFailAtBoth()
	{
		ValidationProblemDetails problem = await CreateInvalidAsync(Valid with { MinTemperatureC = 30, MaxTemperatureC = 10, });

		Assert.Equal(
			[("maxTemperatureC", "maxTemperatureC.belowMin"), ("minTemperatureC", "minTemperatureC.aboveMax"),],
			problem.Violations.Select(violation => (violation.Key, violation.Code)).Order());
	}

	[Fact]
	public async Task InGermanTheMessagesAndTheNamesInThemAreGerman()
	{
		ValidationProblemDetails problem = await CreateInvalidAsync(
			Valid with { Code = "abc", Nickname = "Ada 12 Ada 34 Ada Lovelace", },
			"de");

		Assert.Equal(["Drei Großbuchstaben.",], problem.Errors["code"]);
		Assert.Contains("Der Spitzname darf keine Ziffern enthalten.", problem.Errors["nickname"]);
		Assert.Contains(problem.Errors["nickname"], message => message.Contains("'Spitzname'", StringComparison.Ordinal));
	}

	[Fact]
	public async Task TheHandlerAnswersAReservedNameWithAValidationProblemOfItsOwn()
	{
		ValidationProblemDetails problem = await CreateInvalidAsync(Valid with { Name = "Reserved", });

		Violation reserved = Assert.Single(problem.Violations);
		Assert.Equal(("name", "name.reserved"), (reserved.Key, reserved.Code));
	}

	private DemoProfilesClient Client(string? language = null)
	{
		HttpClient client = Factory.CreateAuthenticatedClient();
		if (language is not null)
		{
			client.DefaultRequestHeaders.AcceptLanguage.ParseAdd(language);
		}

		return new DemoProfilesClient(client);
	}

	private async Task<ValidationProblemDetails> CreateInvalidAsync(DemoProfileRequest request, string? language = null) =>
		await Client(language).CreateAsync(request, Cancellation) switch
		{
			ValidationProblem problem => problem.Problem,
			DemoProfileResponse profile => throw new XunitException($"Expected a validation problem, got {profile}."),
		};
}