using System.Globalization;
using FocusTemplate.Localization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.DependencyInjection;

namespace FocusTemplate.Api.Hosting.Localization;

public static class WebApplicationBuilderExtensions
{
	// The languages the app speaks, its default first. The host registers its own texts and runs UseRequestLocalization()
	public static WebApplicationBuilder AddApiLocalization(this WebApplicationBuilder builder, IReadOnlyList<CultureInfo> cultures)
	{
		builder.Services.AddResxLocalization();

		builder.Services.Configure<RequestLocalizationOptions>(options =>
		{
			options.DefaultRequestCulture = new RequestCulture(cultures[0]);
			options.SupportedCultures = [.. cultures,];
			options.SupportedUICultures = [.. cultures,];

			// The default providers let a query string or a cookie win over Accept-Language, where every caller states its
			// language (the Web app through AcceptLanguageHandler)
			options.RequestCultureProviders = [new AcceptLanguageHeaderRequestCultureProvider(),];
		});

		ValidationTexts.UseResources();

		return builder;
	}
}