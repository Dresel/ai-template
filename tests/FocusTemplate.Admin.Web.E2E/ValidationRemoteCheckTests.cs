namespace FocusTemplate.Admin.Web.E2E;

// The code asked about on the server while the user types, before any submit
[Collection(AspireCollection.Name)]
public sealed class ValidationRemoteCheckTests(BlazorAppFixture app) : ValidationDemoTest(app)
{
	[Fact]
	public async Task ATakenCodeShowsWhileTypingWithoutASubmit()
	{
		await OpenAsync();

		await Input("code").FillAsync("TAK");

		await Expect(Error("code")).ToHaveTextAsync("The code is taken.");
		await Expect(Input("code")).ToHaveAttributeAsync("aria-invalid", "true");
	}

	[Fact]
	public async Task OnlyACodeThatPassesTheFormsOwnRulesIsAskedAbout()
	{
		List<string> asked = [];
		Page.Request += (_, request) =>
		{
			if (request.Url.Contains("/demo-profiles/codes/", StringComparison.Ordinal))
			{
				asked.Add(request.Url[(request.Url.LastIndexOf('/') + 1)..]);
			}
		};
		await OpenAsync();

		await Input("code").FillAsync("ta");
		await Expect(Error("code")).ToHaveTextAsync("Three upper-case letters.");
		await Input("code").FillAsync("TAK");
		await Expect(Error("code")).ToHaveTextAsync("The code is taken.");

		Assert.Equal(["TAK",], asked);
	}

	[Fact]
	public async Task TypingAnotherCodeClearsWhatTheCheckFound()
	{
		await OpenAsync();
		await Input("code").FillAsync("TAK");
		await Expect(Error("code")).ToBeVisibleAsync();

		await Input("code").FillAsync("ABC");

		await Expect(Error("code")).ToBeHiddenAsync();
	}
}