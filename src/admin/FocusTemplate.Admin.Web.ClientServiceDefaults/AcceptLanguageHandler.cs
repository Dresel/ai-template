using System.Globalization;
using System.Net.Http.Headers;

#pragma warning disable IDE0130
namespace Microsoft.Extensions.Hosting;
#pragma warning restore IDE0130

// The API answers in the language the user chose in the app, which the browser's own Accept-Language does not know
internal sealed class AcceptLanguageHandler : DelegatingHandler
{
	protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		request.Headers.AcceptLanguage.Clear();
		request.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue(CultureInfo.CurrentUICulture.Name));

		return base.SendAsync(request, cancellationToken);
	}
}