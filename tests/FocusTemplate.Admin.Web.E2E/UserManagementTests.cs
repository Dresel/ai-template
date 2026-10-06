using Microsoft.Playwright;

namespace FocusTemplate.Admin.Web.E2E;

// As the developer, whom the dev seed makes an administrator. Groups get names of their own, since the whole collection
// shares one database.
[Collection(AspireCollection.Name)]
public sealed class UserManagementTests(BlazorAppFixture app) : BffPageTest(app)
{
	[Fact]
	public async Task AGroupIsCreatedFilledRenamedAndDeleted()
	{
		string name = $"E2E {Guid.NewGuid():N}";
		string renamed = $"{name} renamed";

		await OpenAsync("user-management/groups", "new-group");
		await Page.GetByTestId("new-group").ClickAsync();
		await Page.GetByTestId("group-name").FillAsync(name);
		await Page.GetByTestId("group-description").FillAsync("Made by the end-to-end tests");
		await Page.GetByTestId("save-group").ClickAsync();
		await Expect(Page.GetByTestId("group-title")).ToHaveTextAsync(name);

		// Key by key, as a user types: the autocomplete searches on its key events, which a fill does not raise
		await Page.GetByTestId("member-picker").PressSequentiallyAsync(BlazorAppFixture.Username);
		await Page.GetByRole(AriaRole.Option, new PageGetByRoleOptions { Name = BlazorAppFixture.DisplayName, })
			.ClickAsync();
		await Page.GetByTestId("add-member").ClickAsync();
		await Expect(Page.GetByTestId("member-row")).ToContainTextAsync(BlazorAppFixture.DisplayName);

		await Page.GetByTestId("permission-UserManagement.ViewUsers").ClickAsync();
		await Page.GetByTestId("save-permissions").ClickAsync();
		await Expect(Page.GetByTestId("permissions-saved")).ToBeVisibleAsync();
		await Page.ReloadAsync();
		await Expect(Permission("UserManagement.ViewUsers"))
			.ToBeCheckedAsync(new LocatorAssertionsToBeCheckedOptions { Timeout = 30_000, });
		await Expect(Permission("UserManagement.ManageUsers")).Not.ToBeCheckedAsync();

		await Page.GetByTestId("edit-group").ClickAsync();
		await Page.GetByTestId("group-name").FillAsync(renamed);
		await Page.GetByTestId("save-group").ClickAsync();
		await Expect(Page.GetByTestId("group-title")).ToHaveTextAsync(renamed);

		await Page.GetByTestId("member-row").GetByTestId("remove-member").ClickAsync();
		await ConfirmAsync("Remove");
		await Expect(Page.GetByTestId("member-row")).ToHaveCountAsync(0);

		await Page.GetByTestId("delete-group").ClickAsync();
		await ConfirmAsync("Delete");
		await Expect(Page).ToHaveURLAsync($"{App.BaseUrl}user-management/groups");
		await Expect(Page.GetByTestId("groups-table")).ToBeVisibleAsync();
		await Expect(Page.GetByTestId("group-row").Filter(new LocatorFilterOptions { HasText = name, }))
			.ToHaveCountAsync(0);
	}

	// A signed-in user the page refuses would otherwise go to Keycloak, come straight back with the same session, and loop
	[Fact]
	public async Task AUserWithoutPermissionsIsTurnedAwayRatherThanSentToLogInAgain()
	{
		string userName = $"nobody-{Guid.NewGuid():N}";
		const string password = "nobody-has-a-password";

		await using KeycloakAdmin keycloak = await App.SignInToKeycloakAsync(Playwright);
		await keycloak.CreateUserAsync(userName, password);

		await using IBrowserContext context = await Browser.NewContextAsync(
			new BrowserNewContextOptions { IgnoreHTTPSErrors = true, });
		IPage page = await context.NewPageAsync();
		await page.GotoAsync($"{App.BaseUrl}user-management/users");
		await BlazorAppFixture.LogInAsync(page, userName, password);

		await Expect(page.GetByTestId("forbidden")).ToBeVisibleAsync();
		await Expect(page.GetByTestId("nav-user-management")).ToHaveCountAsync(0);
		await Assert.ThrowsAnyAsync<TimeoutException>(() => page.WaitForRequestAsync(
			request => request.Url.Contains("/bff/login", StringComparison.Ordinal),
			new PageWaitForRequestOptions { Timeout = 5000, }));
	}

	// The API refuses the last active administrator's leaving, and the page says so instead of failing
	[Fact]
	public async Task TheAdministratorsKeepTheirPermissionsAndTheirLastAdministrator()
	{
		await OpenAsync("user-management/groups", "groups-table");
		await Page.GetByTestId("group-row")
			.Filter(new LocatorFilterOptions { HasText = "Administrators", })
			.ClickAsync();

		await Expect(Page.GetByTestId("group-managed")).ToBeVisibleAsync();
		await Expect(Page.GetByTestId("delete-group")).ToBeDisabledAsync();
		await Expect(Page.GetByTestId("save-permissions")).ToHaveCountAsync(0);
		await Expect(Permission("UserManagement.ManageGroups")).ToBeDisabledAsync();

		await Page.GetByTestId("member-row")
			.Filter(new LocatorFilterOptions { HasText = BlazorAppFixture.DisplayName, })
			.GetByTestId("remove-member")
			.ClickAsync();
		await ConfirmAsync("Remove");

		await Expect(Page.GetByTestId("group-failure")).ToContainTextAsync("last active administrator");
		await Expect(
				Page.GetByTestId("member-row")
					.Filter(new LocatorFilterOptions { HasText = BlazorAppFixture.DisplayName, }))
			.ToHaveCountAsync(1);
	}

	[Fact]
	public async Task TheUserListFindsTheDeveloperAndWhatTheirGroupsGrant()
	{
		await OpenAsync("user-management/users", "users-table");

		// The first page shows without a search, ten of the dev seed's users
		await Expect(Page.GetByTestId("user-row")).ToHaveCountAsync(10);

		await Page.GetByTestId("users-search").FillAsync("devel");
		ILocator developer = Page.GetByTestId("user-row")
			.Filter(new LocatorFilterOptions { HasText = BlazorAppFixture.DisplayName, });
		await developer.ClickAsync();

		await Expect(Page.GetByTestId("user-title")).ToHaveTextAsync(BlazorAppFixture.DisplayName);
		await Expect(Page.GetByTestId("user-groups")).ToContainTextAsync("Administrators");
		await Expect(Page.GetByTestId("user-permissions")).ToContainTextAsync("Manage groups");

		// The page does not offer it, and the API would refuse it anyway
		await Expect(Page.GetByTestId("deactivate-user")).ToHaveCountAsync(0);
	}

	// Radzen's confirmation is an alert dialog, which offers the action's own name on its button
	private Task ConfirmAsync(string action) =>
		Page.GetByRole(AriaRole.Alertdialog)
			.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = action, Exact = true, })
			.ClickAsync();

	// The first visit loads the WebAssembly runtime
	private async Task OpenAsync(string path, string testId)
	{
		await Page.GotoAsync($"{App.BaseUrl}{path}");
		await Expect(Page.GetByTestId(testId))
			.ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 30_000, });
	}

	// Radzen's checkbox is the element that carries the test id, states it in aria-checked and aria-disabled
	private ILocator Permission(string name) => Page.GetByTestId($"permission-{name}");
}