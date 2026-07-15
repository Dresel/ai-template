using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

#pragma warning disable IDE0130
namespace Microsoft.Extensions.Hosting;
#pragma warning restore IDE0130

public static class AnalyticsExtensions
{
	public static WebAssemblyHost UseAnalytics(this WebAssemblyHost host)
	{
		IConfiguration configuration = host.Services.GetRequiredService<IConfiguration>();

		string? scriptUrl = configuration["Analytics:ScriptUrl"];
		string? websiteId = configuration["Analytics:WebsiteId"];

		if (string.IsNullOrEmpty(scriptUrl) || string.IsNullOrEmpty(websiteId))
		{
			return host;
		}

		IJSInProcessRuntime jsRuntime = (IJSInProcessRuntime)host.Services.GetRequiredService<IJSRuntime>();
		jsRuntime.InvokeVoid("blazorClientAnalytics.load", scriptUrl, websiteId);

		return host;
	}
}