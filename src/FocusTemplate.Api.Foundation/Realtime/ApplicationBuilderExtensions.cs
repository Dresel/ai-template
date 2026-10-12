using Microsoft.AspNetCore.Builder;

namespace FocusTemplate.Api.Foundation.Realtime;

public static class ApplicationBuilderExtensions
{
	// Asks nginx to pass each server-sent event on at once, whatever buffering the ingress has configured. The BFF
	// passes the header on to the ingress
	public static IApplicationBuilder UseUnbufferedEventStreams(this IApplicationBuilder app) =>
		app.Use((context, next) =>
		{
			context.Response.OnStarting(() =>
			{
				if (context.Response.ContentType?.StartsWith("text/event-stream", StringComparison.OrdinalIgnoreCase) == true)
				{
					context.Response.Headers["X-Accel-Buffering"] = "no";
				}

				return Task.CompletedTask;
			});

			return next(context);
		});
}