using System.Net;
using Microsoft.AspNetCore.Components;

#pragma warning disable IDE0130
namespace Microsoft.Extensions.Hosting;
#pragma warning restore IDE0130

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