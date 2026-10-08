using System.Net;
using System.Text;

namespace FocusTemplate.Admin.Web.UnitTests.Features.UserManagement.Groups;

// Answers with an empty event stream and keeps the request, so a test reads the options the client set
public sealed class TestWatchHandler : HttpMessageHandler
{
	public HttpRequestMessage? Request { get; private set; }

	protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		Request = request;

		return Task.FromResult(
			new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(string.Empty, Encoding.UTF8, "text/event-stream"), });
	}
}