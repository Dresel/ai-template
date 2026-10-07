using FluentValidation;
using FluentValidation.Internal;
using FluentValidation.Results;
using FocusTemplate.Admin.Client.DemoProfiles;
using FocusTemplate.Admin.Shared;
using FocusTemplate.Admin.Shared.Localization;
using FocusTemplate.Admin.Web.Features.DemoProfiles;

namespace FocusTemplate.Admin.Web.UnitTests.Features.DemoProfiles;

public sealed class FormTests : IDisposable
{
	private readonly TestCodesHandler codes = new();

	private readonly HttpClient http;

	private readonly FormValidator validator;

	public FormTests()
	{
		this.http = new HttpClient(this.codes) { BaseAddress = new Uri("http://localhost/"), };
		this.validator = new FormValidator(new DemoProfilesClient(this.http), Localizations.Get<IDemoProfileCustomRulesLocalizations>());
	}

	// The store shows a run's messages as soon as it completes: one that reaches no async rule completes at once
	[Fact]
	public void ACodeFailingItsOwnRulesIsValidatedAtOnce()
	{
		Task<ValidationResult> run = this.validator.ValidateAsync(
			new Form { Code = "ta", },
			OnlyTheCode,
			TestContext.Current.CancellationToken);

		Assert.True(run.IsCompletedSuccessfully);
		Assert.Empty(this.codes.Asked);
	}

	[Fact]
	public async Task ACodeIsAskedAboutOnlyOnceItPassesItsOwnRules()
	{
		Form form = new() { Code = "ta", };

		Assert.DoesNotContain(await ErrorCodesAtCodeAsync(form), code => code == "code.taken");
		form.Code = "TAK";
		Assert.Contains(await ErrorCodesAtCodeAsync(form), code => code == "code.taken");

		Assert.Equal(["TAK",], this.codes.Asked);
	}

	[Fact]
	public void AnIncompleteFormStillMapsSoTheServerCanSayWhatIsMissing()
	{
		DemoProfileRequest request = new Form().ToContract();

		Assert.Null(request.Code);
		Assert.Null(request.Address.Street);
		Assert.Equal(0, request.Age);
	}

	[Fact]
	public async Task CrossedTemperaturesFailAtBoth()
	{
		Form form = new() { MinTemperatureC = 30, MaxTemperatureC = 10, };

		IEnumerable<(string, string)> failures =
			(await this.validator.ValidateAsync(form, OnlyTheTemperatures, TestContext.Current.CancellationToken)).Errors
			.Select(failure => (failure.PropertyName, failure.ErrorCode))
			.Order();

		Assert.Equal([("MaxTemperatureC", "maxTemperatureC.belowMin"), ("MinTemperatureC", "minTemperatureC.aboveMax"),], failures);
	}

	public void Dispose()
	{
		this.http.Dispose();
		this.codes.Dispose();
	}

	[Fact]
	public void EachTemperatureIsValidatedWithTheOther()
	{
		Assert.Equal(["MaxTemperatureC",], this.validator.Dependencies["MinTemperatureC"]);
		Assert.Equal(["MinTemperatureC",], this.validator.Dependencies["MaxTemperatureC"]);
	}

	[Fact]
	public async Task InGermanTheMessagesAndTheNamesInThemAreGerman()
	{
		ValidationTexts.UseResources();
		using UiCulture german = UiCulture.Use("de");
		Form form = new() { Code = "abc", Nickname = "Ada 12", };

		ValidationResult result = await this.validator.ValidateAsync(
			form,
			options => options.IncludeProperties(nameof(Form.Code), nameof(Form.Nickname), nameof(Form.Age)),
			TestContext.Current.CancellationToken);

		Assert.Contains(result.Errors, failure => failure is { PropertyName: "Code", ErrorMessage: "Drei Großbuchstaben.", });
		Assert.Contains(
			result.Errors,
			failure => failure is { PropertyName: "Nickname", ErrorMessage: "Der Spitzname darf keine Ziffern enthalten.", });
		Assert.Equal(
			"Alter",
			Assert.Single(result.Errors, failure => failure.PropertyName == "Age").FormattedMessagePlaceholderValues["PropertyName"]);
	}

	[Fact]
	public async Task TheFormsFailuresAreKeyedByThePathsItsInputsRegister()
	{
		Form form = new()
		{
			Code = "abc", DisplayName = new string('n', 51), Age = 17, WeightKg = 0,
		};
		form.Address.PostalCode = "119";
		form.Tags.AddRange(
			[new TagRow { Value = "ok", }, new TagRow(), .. Enumerable.Range(0, 4).Select(_ => new TagRow { Value = "x", }),]);

		IEnumerable<string> paths = (await this.validator.ValidateAsync(form, TestContext.Current.CancellationToken)).Errors
			.Where(failure => failure.Severity == Severity.Error)
			.Select(failure => failure.PropertyName)
			.Distinct()
			.Order(StringComparer.Ordinal);

		string[] expected =
		[
			"Address.City", "Address.PostalCode", "Address.Street", "Age", "Code", "DisplayName", "Tags", "Tags[1]", "WeightKg",
		];
		Assert.Equal(expected, paths);
	}

	[Fact]
	public void ToContractRenamesTheDisplayNameAndUnwrapsTheTagRows()
	{
		Form form = new() { Code = "ABC", DisplayName = "Ada", Age = 36, };
		(form.Address.Street, form.Address.PostalCode, form.Address.City) = ("Hohe Warte 38", "1190", "Vienna");
		form.Tags.AddRange([new TagRow { Value = "blue", }, new TagRow(),]);

		DemoProfileRequest request = form.ToContract();

		Assert.Equal(("ABC", "Ada", 36), (request.Code, request.Name, request.Age));
		Assert.Equal(new DemoAddressRequest("Hohe Warte 38", "1190", "Vienna"), request.Address);
		Assert.Equal(["blue", string.Empty,], request.Tags);
	}

	[Fact]
	public async Task WithTheDemosSwitchOffTheFormsRulesFindNothing()
	{
		Form form = new() { Code = "TAK", ClientRules = false, };

		Assert.True((await this.validator.ValidateAsync(form, TestContext.Current.CancellationToken)).IsValid);
		Assert.Empty(this.codes.Asked);
	}

	private static void OnlyTheCode(ValidationStrategy<Form> options) => options.IncludeProperties(nameof(Form.Code));

	private static void OnlyTheTemperatures(ValidationStrategy<Form> options) =>
		options.IncludeProperties(nameof(Form.MinTemperatureC), nameof(Form.MaxTemperatureC));

	private async Task<IEnumerable<string>> ErrorCodesAtCodeAsync(Form form) =>
		(await this.validator.ValidateAsync(form, OnlyTheCode, TestContext.Current.CancellationToken)).Errors.Select(failure =>
			failure.ErrorCode);
}