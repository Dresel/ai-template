using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Playwright;

namespace FocusTemplate.Admin.Web.E2E;

// Disposing it undoes its changes to the realm: the whole collection shares one Keycloak.
public sealed class KeycloakAdmin(IAPIRequestContext api) : IAsyncDisposable
{
	private const string Realm = "admin/realms/focus";

	private readonly List<string> createdUsers = [];

	private readonly List<(string Client, JsonObject Mapper)> removedMappers = [];

	private int? accessTokenLifespan;

	public static async Task<KeycloakAdmin> SignInAsync(
		IPlaywright playwright,
		Uri keycloak,
		string userName,
		string password)
	{
		// Keycloak serves the ASP.NET dev certificate, which Playwright does not trust.
		string accessToken;
		await using (IAPIRequestContext anonymous = await playwright.APIRequest.NewContextAsync(
			new APIRequestNewContextOptions { BaseURL = keycloak.ToString(), IgnoreHTTPSErrors = true, }))
		{
			IAPIResponse response = await EnsureSuccessAsync(
				anonymous.PostAsync(
					"realms/master/protocol/openid-connect/token",
					new APIRequestContextOptions
					{
						Form = anonymous.CreateFormData()
							.Set("grant_type", "password")
							.Set("client_id", "admin-cli")
							.Set("username", userName)
							.Set("password", password),
					}));

			JsonElement token = await response.JsonAsync() ??
				throw new InvalidOperationException("Keycloak answered the token request without a body.");
			accessToken = token.GetProperty("access_token").GetString() ?? string.Empty;
		}

		return new KeycloakAdmin(
			await playwright.APIRequest.NewContextAsync(
				new APIRequestNewContextOptions
				{
					BaseURL = keycloak.ToString(),
					ExtraHTTPHeaders =
						new Dictionary<string, string> { ["Authorization"] = $"Bearer {accessToken}", },
					IgnoreHTTPSErrors = true,
				}));
	}

	// The profile is complete, so Keycloak asks for nothing more at the first login
	public async Task CreateUserAsync(string userName, string password)
	{
		IAPIResponse response = await EnsureSuccessAsync(
			api.PostAsync(
				$"{Realm}/users",
				new APIRequestContextOptions
				{
					DataObject = new
					{
						username = userName,
						enabled = true,
						email = $"{userName}@focus.local",
						emailVerified = true,
						firstName = "End",
						lastName = "To End",
						credentials =
							new[] { new { type = "password", value = password, temporary = false, }, },
					},
				}));

		// Keycloak answers with the new user's URL, the id its last segment
		string location = response.Headers["location"];
		this.createdUsers.Add(location[(location.LastIndexOf('/') + 1)..]);
	}

	public async ValueTask DisposeAsync()
	{
		foreach (string user in this.createdUsers)
		{
			await EnsureSuccessAsync(api.DeleteAsync($"{Realm}/users/{user}"));
		}

		foreach ((string client, JsonObject mapper) in this.removedMappers)
		{
			await EnsureSuccessAsync(
				api.PostAsync(
					$"{Realm}/clients/{client}/protocol-mappers/models",
					new APIRequestContextOptions { DataObject = mapper, }));
		}

		if (this.accessTokenLifespan is { } lifespan)
		{
			await SetAccessTokenLifespanAsync(lifespan);
		}

		await api.DisposeAsync();
	}

	public Task EndSessionAsync(string sessionId) =>
		EnsureSuccessAsync(api.DeleteAsync($"{Realm}/sessions/{Uri.EscapeDataString(sessionId)}"));

	public async Task RemoveProtocolMapperAsync(string clientId, string mapperName)
	{
		string client = await InternalClientIdAsync(clientId);
		string models = $"{Realm}/clients/{client}/protocol-mappers/models";

		IAPIResponse response = await EnsureSuccessAsync(api.GetAsync(models));
		JsonElement mappers = await response.JsonAsync() ??
			throw new InvalidOperationException("Keycloak answered the protocol mapper request without a body.");
		JsonElement mapper = mappers.EnumerateArray()
			.Single(candidate => candidate.GetProperty("name").GetString() == mapperName);

		await EnsureSuccessAsync(api.DeleteAsync($"{models}/{mapper.GetProperty("id").GetString()}"));

		// Keycloak assigns a new id when it is added back.
		JsonObject restore = JsonNode.Parse(mapper.GetRawText())?.AsObject() ??
			throw new InvalidOperationException("Keycloak answered with a protocol mapper that is no object.");
		restore.Remove("id");
		this.removedMappers.Add((client, restore));
	}

	// 30 s lies within the token management's one-minute renewal window, so every use renews the token.
	public async Task RenewAccessTokensOnEveryUseAsync()
	{
		IAPIResponse response = await EnsureSuccessAsync(api.GetAsync(Realm));
		JsonElement realm = await response.JsonAsync() ??
			throw new InvalidOperationException("Keycloak answered the realm request without a body.");

		this.accessTokenLifespan ??= realm.GetProperty("accessTokenLifespan").GetInt32();
		await SetAccessTokenLifespanAsync(30);
	}

	private static async Task<IAPIResponse> EnsureSuccessAsync(Task<IAPIResponse> request)
	{
		IAPIResponse response = await request;

		return response.Ok
			? response
			: throw new InvalidOperationException($"Keycloak answered {response.Status}: {await response.TextAsync()}");
	}

	// The admin API wants the client's UUID, not its client id.
	private async Task<string> InternalClientIdAsync(string clientId)
	{
		IAPIResponse response =
			await EnsureSuccessAsync(api.GetAsync($"{Realm}/clients?clientId={Uri.EscapeDataString(clientId)}"));
		JsonElement clients = await response.JsonAsync() ??
			throw new InvalidOperationException("Keycloak answered the client request without a body.");

		return clients.EnumerateArray().Single().GetProperty("id").GetString() ?? string.Empty;
	}

	// Keycloak updates only the fields sent.
	private async Task SetAccessTokenLifespanAsync(int seconds) =>
		await EnsureSuccessAsync(
			api.PutAsync(Realm, new APIRequestContextOptions { DataObject = new { accessTokenLifespan = seconds, }, }));
}