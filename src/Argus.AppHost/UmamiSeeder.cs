using System.Text.Json;

namespace Argus.AppHost;

internal static class UmamiSeeder
{
	public static async Task<string?> EnsureWebsiteAsync(
		string umamiBaseUrl,
		string websiteName,
		string websiteDomain,
		CancellationToken cancellationToken)
	{
		try
		{
			using HttpClient client = new();
			client.BaseAddress = new Uri(umamiBaseUrl);

			using HttpResponseMessage loginResponse = await client.PostAsJsonAsync(
				"/api/auth/login",
				new { username = "admin", password = "umami", },
				cancellationToken);

			if (!loginResponse.IsSuccessStatusCode)
			{
				return null;
			}

			using JsonDocument loginJson = JsonDocument.Parse(await loginResponse.Content.ReadAsStringAsync(cancellationToken));
			string? token = loginJson.RootElement.GetProperty("token").GetString();
			client.DefaultRequestHeaders.Authorization = new("Bearer", token);

			string? existingId = await FindWebsiteAsync(client, websiteName, cancellationToken);
			if (existingId is not null)
			{
				return existingId;
			}

			using HttpResponseMessage createResponse = await client.PostAsJsonAsync(
				"/api/websites",
				new { name = websiteName, domain = websiteDomain, },
				cancellationToken);

			if (!createResponse.IsSuccessStatusCode)
			{
				return null;
			}

			using JsonDocument createJson = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync(cancellationToken));
			return createJson.RootElement.GetProperty("id").GetString();
		}
		catch (Exception ex) when (ex is HttpRequestException or JsonException or KeyNotFoundException or TaskCanceledException)
		{
			return null;
		}
	}

	private static async Task<string?> FindWebsiteAsync(HttpClient client, string websiteName, CancellationToken cancellationToken)
	{
		using HttpResponseMessage response = await client.GetAsync("/api/websites", cancellationToken);
		if (!response.IsSuccessStatusCode)
		{
			return null;
		}

		using JsonDocument json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));

		JsonElement websites = json.RootElement.ValueKind == JsonValueKind.Array
			? json.RootElement
			: json.RootElement.GetProperty("data");

		foreach (JsonElement website in websites.EnumerateArray())
		{
			if (website.GetProperty("name").GetString() == websiteName)
			{
				return website.GetProperty("id").GetString();
			}
		}

		return null;
	}
}