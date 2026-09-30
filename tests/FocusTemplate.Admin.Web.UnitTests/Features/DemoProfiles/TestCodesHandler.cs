using System.Net;
using System.Text;

namespace FocusTemplate.Admin.Web.UnitTests.Features.DemoProfiles;

// The code check's endpoint as the demo API answers it, TAK taken, recording the codes asked about
public sealed class TestCodesHandler : HttpMessageHandler
{
	public List<string> Asked { get; } = [];

	protected override Task<HttpResponseMessage> SendAsync(
		HttpRequestMessage request,
		CancellationToken cancellationToken)
	{
		string code = request.RequestUri!.Segments[^1];
		Asked.Add(code);

		string body = $$"""{"code":"{{code}}","taken":{{(code == "TAK" ? "true" : "false")}}}""";
		return Task.FromResult(
			new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(body, Encoding.UTF8, "application/json"),
			});
	}
}