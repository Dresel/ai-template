#pragma warning disable IDE0130
namespace Microsoft.Extensions.Hosting;
#pragma warning restore IDE0130

// The BFF refuses a proxied API call without this header.
// A cross-site page cannot add it without a CORS preflight the BFF never grants,
// which is what makes the session cookie safe to send along.
internal sealed class CsrfHeaderHandler : DelegatingHandler
{
	protected override Task<HttpResponseMessage> SendAsync(
		HttpRequestMessage request,
		CancellationToken cancellationToken)
	{
		request.Headers.TryAddWithoutValidation("X-CSRF", "1");
		return base.SendAsync(request, cancellationToken);
	}
}