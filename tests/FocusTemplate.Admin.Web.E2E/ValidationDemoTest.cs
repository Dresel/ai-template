using Microsoft.Playwright;

namespace FocusTemplate.Admin.Web.E2E;

// The validation demo: one profile form, each input and message found by its data-testid. Messages the server phrases
// through FluentValidation's built-in texts follow its culture, so tests compare those only where the browser phrased them
public abstract class ValidationDemoTest(BlazorAppFixture app) : BffPageTest(app)
{
	protected static readonly string TooLongName = new('n', 51);

	protected ILocator Accepted => Page.GetByTestId("accepted");

	protected ILocator Failure => Page.GetByTestId("submit-failure");

	// The dialog a field's line opens, its messages grouped by severity
	protected ILocator MessagesDialog => Page.GetByTestId("field-messages");

	protected ILocator Summary => Page.GetByTestId("validation-summary");

	protected async Task AddTagsAsync(params string[] tags)
	{
		int first = await Page.GetByTestId("tag-row").CountAsync();
		for (int index = 0; index < tags.Length; index++)
		{
			await Page.GetByTestId("add-tag").ClickAsync();
			await Input($"tag-{first + index}").FillAsync(tags[index]);
		}
	}

	protected Task CheckOnlyOnTheServerAsync() => SwitchAsync("client-rules", false);

	protected ILocator Error(string input) => Page.GetByTestId($"{input}-error");

	// A profile the server accepts, before a test breaks what it is about
	protected async Task FillValidAsync()
	{
		await TypeAsync("code", "ABC");
		await TypeAsync("name", "Ada");
		await TypeAsync("age", "36");
		await TypeAsync("street", "Hohe Warte 38");
		await TypeAsync("postal-code", "1190");
		await TypeAsync("city", "Vienna");
	}

	protected ILocator Info(string input) => Page.GetByTestId($"{input}-info");

	protected ILocator Input(string input) => Page.GetByTestId(input);

	// The pill counting what a field's line does not show, a button to all of them
	protected ILocator More(string input) => Page.GetByTestId($"{input}-more");

	// The first visit loads the WebAssembly runtime
	protected async Task OpenAsync()
	{
		await Page.GotoAsync($"{App.BaseUrl}demo/validation");
		await Expect(Input("code")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 30_000, });
	}

	protected Task SubmitAsync() => Page.GetByTestId("submit").ClickAsync();

	// Radzen puts the test id on the switch's track, which a user clicks, and states it on the hidden input inside
	protected async Task SwitchAsync(string testId, bool on)
	{
		ILocator toggle = Page.GetByTestId(testId);
		await toggle.ClickAsync();
		await (on
			? Expect(toggle.GetByRole(AriaRole.Switch)).ToBeCheckedAsync()
			: Expect(toggle.GetByRole(AriaRole.Switch)).Not.ToBeCheckedAsync());
	}

	// The Radzen inputs take a value on every keystroke
	protected Task TypeAsync(string input, string value) => Input(input).FillAsync(value);

	protected ILocator Warning(string input) => Page.GetByTestId($"{input}-warning");
}