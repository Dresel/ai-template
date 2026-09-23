using System.Net.Http.Headers;

#pragma warning disable IDE0130
namespace Microsoft.Extensions.Hosting;
#pragma warning restore IDE0130

// Captures the parts of an HttpRequestMessage that a retry needs, because the SDK disposes the original while the
// background send is still running. The OTLP SDK always sends ByteArrayContent (protobuf), so ReadAsByteArrayAsync
// completes synchronously.
internal sealed class RequestSnapshot
{
	public byte[]? ContentBytes { get; init; }

	public MediaTypeHeaderValue? ContentType { get; init; }

	public List<KeyValuePair<string, IEnumerable<string>>> Headers { get; init; } = null!;

	public HttpMethod Method { get; init; } = null!;

	public Uri RequestUri { get; init; } = null!;

	public static RequestSnapshot Capture(HttpRequestMessage request)
	{
		byte[]? contentBytes = null;
		MediaTypeHeaderValue? contentType = null;

		if (request.Content is not null)
		{
			// ByteArrayContent.ReadAsByteArrayAsync completes synchronously
			// since the bytes are already in memory — safe to .GetResult().
			contentBytes = request.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
			contentType = request.Content.Headers.ContentType;
		}

		// Copy headers since the original request will be disposed.
		List<KeyValuePair<string, IEnumerable<string>>> headers = [];
		foreach (KeyValuePair<string, IEnumerable<string>> header in request.Headers)
		{
			headers.Add(new KeyValuePair<string, IEnumerable<string>>(header.Key, [.. header.Value,]));
		}

		return new RequestSnapshot
		{
			Method = request.Method,
			RequestUri = request.RequestUri!,
			Headers = headers,
			ContentBytes = contentBytes,
			ContentType = contentType,
		};
	}

	public HttpRequestMessage CreateRequest()
	{
		HttpRequestMessage clone = new(Method, RequestUri);

		foreach (KeyValuePair<string, IEnumerable<string>> header in Headers)
		{
			clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
		}

		if (ContentBytes is not null)
		{
			clone.Content = new ByteArrayContent(ContentBytes);
			if (ContentType is not null)
			{
				clone.Content.Headers.ContentType = ContentType;
			}
		}

		return clone;
	}
}