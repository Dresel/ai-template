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
}