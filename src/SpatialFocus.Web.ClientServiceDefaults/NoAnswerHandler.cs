using Polly;

#pragma warning disable IDE0130
namespace Microsoft.Extensions.Hosting;
#pragma warning restore IDE0130

// The resilience handler says "no answer" in Polly's exceptions. Above it, the clients know only HttpRequestException,
// so a client stays free of the pipeline it runs in
internal sealed class NoAnswerHandler : DelegatingHandler
{
	protected override async Task<HttpResponseMessage> SendAsync(
		HttpRequestMessage request,
		CancellationToken cancellationToken)
	{
		try
		{
			return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
		}
		catch (ExecutionRejectedException exception)
		{
			throw new HttpRequestException("The resilience handler gave up on the request.", exception);
		}
	}
}