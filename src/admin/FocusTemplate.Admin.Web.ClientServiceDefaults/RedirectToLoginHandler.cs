using System.Net;
using Microsoft.AspNetCore.Components;

#pragma warning disable IDE0130
namespace Microsoft.Extensions.Hosting;
#pragma warning restore IDE0130

// A 401 from a proxied API means the session has no usable token any more, so the user has to log in again. A full load,
// because bff/login is a server endpoint outside the client router.
internal sealed class RedirectToLoginHandler(NavigationManager navigation) : DelegatingHandler
{
	protected override async Task<HttpResponseMessage> SendAsync(
		HttpRequestMessage request,
		CancellationToken cancellationToken)
	{
		HttpResponseMessage response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

		if (response.StatusCode == HttpStatusCode.Unauthorized)
		{
			navigation.NavigateTo(
				$"bff/login?returnUrl={Uri.EscapeDataString("/" + navigation.ToBaseRelativePath(navigation.Uri))}",
				true);
		}

		return response;
	}
}