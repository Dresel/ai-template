using System.Net;
using System.Text.Json;
using FocusTemplate.Admin.Shared;
using FocusTemplate.Admin.Shared.UserManagement;
using FocusTemplate.Primitives;

namespace FocusTemplate.Admin.Web.UnitTests.Features.UserManagement.Users;

// The user search, answering a request only when the test says so, so the test decides which one finishes first
public sealed class TestUsersHandler : HttpMessageHandler
{
	private static readonly JsonSerializerOptions Options =
		new(JsonSerializerDefaults.Web) { TypeInfoResolver = AdminJsonContext.Default, };

	private readonly List<(TaskCompletionSource<HttpResponseMessage> Response, CancellationToken Cancellation)>
		requests = [];

	public List<UserSearchRequest> Requests { get; } = [];

	public void Answer(int request, params string[] displayNames)
	{
		UserPageResponse page = new(
			[
				.. displayNames.Select(name => new UserSummaryResponse(
					UserId.From(Guid.CreateVersion7()),
					name,
					true,
					DateTimeOffset.UnixEpoch)),
			],
			displayNames.Length);

		this.requests[request]
			.Response.SetResult(
				new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(page, options: Options), });
	}

	public void Fail(int request, HttpStatusCode status) =>
		this.requests[request].Response.SetResult(new HttpResponseMessage(status));

	public bool WasCanceled(int request) => this.requests[request].Cancellation.IsCancellationRequested;

	protected override Task<HttpResponseMessage> SendAsync(
		HttpRequestMessage request,
		CancellationToken cancellationToken)
	{
		// Read at once, so the request is recorded before the caller's next line asks about it
		string body = request.Content!.ReadAsStringAsync(cancellationToken).GetAwaiter().GetResult();
		Requests.Add(JsonSerializer.Deserialize<UserSearchRequest>(body, Options)!);

		TaskCompletionSource<HttpResponseMessage> response = new(TaskCreationOptions.RunContinuationsAsynchronously);
		cancellationToken.Register(() => response.TrySetCanceled(cancellationToken));
		this.requests.Add((response, cancellationToken));

		return response.Task;
	}
}