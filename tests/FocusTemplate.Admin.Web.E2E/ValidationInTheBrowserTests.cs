namespace FocusTemplate.Admin.Web.E2E;

// The generated rules and the shared ones, run by the form's validator while the user types
[Collection(AspireCollection.Name)]
public sealed class ValidationInTheBrowserTests(BlazorAppFixture app) : ValidationDemoTest(app)
{
	[Fact]
	public async Task AFieldShowsNothingUntilTouchedThenItsErrorAtTheInput()
	{
		await OpenAsync();
		await Expect(Error("name")).ToBeHiddenAsync();

		await Input("name").FillAsync(TooLongName);

		await Expect(Error("name")).ToContainTextAsync("50 characters or fewer");
		await Expect(Input("name")).ToHaveAttributeAsync("aria-invalid", "true");
	}

	[Fact]
	public async Task ANestedMembersErrorShowsAtItsInput()
	{
		await OpenAsync();

		await Input("street").FillAsync(new string('s', 101));

		await Expect(Error("street")).ToContainTextAsync("100 characters or fewer");
	}

	[Fact]
	public async Task APatternShowsTheSpecsOwnMessage()
	{
		await OpenAsync();

		await Input("code").FillAsync("abc");

		await Expect(Error("code")).ToHaveTextAsync("Three upper-case letters.");
	}

	[Fact]
	public async Task ASixthTagFailsAtTheListAndAnEmptyOneAtItsRow()
	{
		await OpenAsync();

		await AddTagsAsync("a", "b", "c", "d", "e", "f");
		await Expect(Error("tags")).ToContainTextAsync("5 items or fewer");

		await Input("tag-1").FillAsync(string.Empty);
		await Expect(Error("tag-1")).ToContainTextAsync("at least 1 characters");
		await Expect(Error("tag-0")).ToBeHiddenAsync();
	}

	[Fact]
	public async Task AnEmptyNumberPassesWhenOptionalAndIsRequiredOtherwise()
	{
		await OpenAsync();

		await Input("weight").FillAsync("5");
		await Input("weight").FillAsync(string.Empty);
		await Input("age").FillAsync("5");
		await Input("age").FillAsync(string.Empty);

		await Expect(Error("age")).ToContainTextAsync("must not be empty");
		await Expect(Error("weight")).ToBeHiddenAsync();
	}

	[Fact]
	public async Task BoundsHoldAtTheirEdgesAndAnExclusiveOneRejectsItself()
	{
		await OpenAsync();

		(string Input, string Value, string? Error)[] steps =
		[
			("age", "17", "greater than or equal to '18'"),
			("age", "18", null),
			("age", "121", "less than or equal to '120'"),
			("age", "120", null),
			("weight", "0", "greater than '0'"),
			("weight", "0.1", null),
		];
		foreach ((string input, string value, string? error) in steps)
		{
			await Input(input).FillAsync(value);
			await (error is null
				? Expect(Error(input)).ToBeHiddenAsync()
				: Expect(Error(input)).ToContainTextAsync(error));
		}
	}

	[Fact]
	public async Task ChangingEitherTemperatureRechecksThePairAtBoth()
	{
		await OpenAsync();

		await Input("max-temperature").FillAsync("10");
		await Input("min-temperature").FillAsync("30");
		await Expect(Error("max-temperature")).ToHaveTextAsync("The highest temperature must not be below the lowest.");
		await Expect(Error("min-temperature")).ToHaveTextAsync("The lowest temperature must not be above the highest.");

		await Input("max-temperature").FillAsync("40");
		await Expect(Error("max-temperature")).ToBeHiddenAsync();
		await Expect(Error("min-temperature")).ToBeHiddenAsync();
	}

	[Fact]
	public async Task FixingTheValueClearsTheErrorRightAway()
	{
		await OpenAsync();
		await Input("name").FillAsync(TooLongName);
		await Expect(Error("name")).ToBeVisibleAsync();

		await Input("name").FillAsync("Ada");

		await Expect(Error("name")).ToBeHiddenAsync();
		await Expect(Input("name")).Not.ToHaveAttributeAsync("aria-invalid", "true");
	}

	[Fact]
	public async Task SubmittingEmptyShowsRequiredAtEachInputAndSendsNothing()
	{
		List<string> posted = [];
		Page.Request += (_, request) =>
		{
			if (request.Url.EndsWith("/demo-profiles", StringComparison.Ordinal))
			{
				posted.Add(request.Method);
			}
		};
		await OpenAsync();

		await SubmitAsync();

		foreach (string input in (string[])["code", "name", "age", "street", "postal-code", "city",])
		{
			await Expect(Error(input)).ToContainTextAsync("must not be empty");
		}

		await Expect(Input("code")).ToBeFocusedAsync();

		// Requests go out in order, so the one a valid submit sends proves the first sent none
		await FillValidAsync();
		await SubmitAsync();
		await Expect(Accepted).ToBeVisibleAsync();
		Assert.Equal(["POST",], posted);
	}

	[Fact]
	public async Task TabbingThroughAnEmptyRequiredFieldShowsThatItIsRequired()
	{
		await OpenAsync();
		await Input("code").FocusAsync();

		await Page.Keyboard.PressAsync("Tab");
		await Expect(Error("code")).ToContainTextAsync("must not be empty");
		await Expect(Input("name")).ToBeFocusedAsync();
		await Expect(Error("name")).ToBeHiddenAsync();

		await Page.Keyboard.PressAsync("Tab");
		await Expect(Error("name")).ToContainTextAsync("must not be empty");
	}

	[Fact]
	public async Task TypingInOneFieldRevealsNothingOnTheOthers()
	{
		await OpenAsync();

		await Input("name").FillAsync(TooLongName);

		await Expect(Error("name")).ToBeVisibleAsync();
		await Expect(Error("code")).ToBeHiddenAsync();
		await Expect(Error("age")).ToBeHiddenAsync();
	}
}