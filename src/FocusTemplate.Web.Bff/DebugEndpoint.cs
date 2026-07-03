using FocusTemplate.Shared;

namespace FocusTemplate.Web.Bff;

internal static class DebugEndpoint
{
	public static WebApplication MapDebugEndpoint(this WebApplication app)
	{
		app.MapGet(
			"/debug/request",
			(HttpContext ctx) => Results.Json(
				new RequestDiagnostics(
					ctx.Request.Scheme,
					ctx.Request.IsHttps,
					ctx.Request.Host.ToString(),
					ctx.Connection.RemoteIpAddress?.ToString(),
					ctx.Request.Headers["X-Forwarded-Proto"].ToString(),
					ctx.Request.Headers["X-Forwarded-Host"].ToString(),
					ctx.Request.Headers["X-Forwarded-For"].ToString(),
					ctx.Request.Headers["X-Original-Proto"].ToString(),
					ctx.Request.Headers["X-Original-For"].ToString())));

		return app;
	}
}