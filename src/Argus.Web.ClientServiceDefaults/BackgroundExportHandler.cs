using System.Net;
using Microsoft.Extensions.Logging;
using Polly;

#pragma warning disable IDE0130
namespace Microsoft.Extensions.Hosting;
#pragma warning restore IDE0130

/// <summary>
///     A DelegatingHandler that works around the OTel SDK's sync-over-async
///     deadlock on WASM. The SDK calls SendAsync().GetAwaiter().GetResult()
///     in OtlpExportClient.SendHttpRequest(), which blocks the single WASM
///     thread. This handler returns 200 immediately to unblock the SDK,
///     then sends the real request with retries in the background.
/// </summary>
internal sealed partial class BackgroundExportHandler(ResiliencePipeline<HttpResponseMessage> pipeline, ILogger logger)
	: DelegatingHandler(new HttpClientHandler())
{
	protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		// Capture request data before returning — the SDK disposes the
		// HttpRequestMessage via 'using' after .GetResult() completes.
		RequestSnapshot snapshot = RequestSnapshot.Capture(request);

		// Send the real request with retries in the background.
		// Use CancellationToken.None since this work is intentionally detached
		// from the caller — the SDK disposes the request immediately after
		// .GetResult() returns, and we want the background send to complete
		// even if the export pipeline's token is cancelled.
		_ = SendWithRetryAsync(snapshot, CancellationToken.None);

		// Return 200 immediately so the SDK's sync .GetResult() unblocks.
		return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
	}

	[LoggerMessage(LogLevel.Warning, "OTLP export to {Uri} failed after retries.")]
	private static partial void LogExportFailure(ILogger logger, Exception exception, Uri uri);

	[LoggerMessage(LogLevel.Warning, "OTLP export to {Uri} completed with status {StatusCode} after retries.")]
	private static partial void LogExportStatusWarning(ILogger logger, Uri uri, HttpStatusCode statusCode);

	private async Task SendWithRetryAsync(RequestSnapshot snapshot, CancellationToken cancellationToken)
	{
		try
		{
			HttpResponseMessage response = await pipeline.ExecuteAsync(
					async token =>
					{
						using HttpRequestMessage clone = snapshot.CreateRequest();
						return await base.SendAsync(clone, token).ConfigureAwait(false);
					},
					cancellationToken)
				.ConfigureAwait(false);

			if (!response.IsSuccessStatusCode)
			{
				LogExportStatusWarning(logger, snapshot.RequestUri, response.StatusCode);
			}

			response.Dispose();
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			LogExportFailure(logger, ex, snapshot.RequestUri);
		}
	}
}