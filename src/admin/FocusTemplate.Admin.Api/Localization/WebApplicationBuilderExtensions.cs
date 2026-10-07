using FocusTemplate.Admin.Shared.Localization;
using Microsoft.AspNetCore.Localization;
using ResXGenerator.Registration;

namespace FocusTemplate.Admin.Api.Localization;

public static class WebApplicationBuilderExtensions
{
	public static WebApplicationBuilder AddApiLocalization(this WebApplicationBuilder builder)
	{
		builder.Services.AddAdminSharedLocalization().UsingResXGenerator();

		builder.Services.Configure<RequestLocalizationOptions>(options =>
		{
			options.DefaultRequestCulture = new RequestCulture(Cultures.Default);
			options.SupportedCultures = [.. Cultures.Supported,];
			options.SupportedUICultures = [.. Cultures.Supported,];

			// The default providers let a query string or a cookie win over Accept-Language, where every caller states its
			// language (the Web app through AcceptLanguageHandler)
			options.RequestCultureProviders = [new AcceptLanguageHeaderRequestCultureProvider(),];
		});

		ValidationTexts.UseResources();

		return builder;
	}
}