using System.Globalization;
using Duende.AccessTokenManagement.OpenIdConnect;
using FocusTemplate.Admin.UserManagement.Client.Users;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Instrumentation.Http;

namespace FocusTemplate.Web.Bff.Hosting;

public static class WebApplicationBuilderExtensions
{
	public static WebApplicationBuilder AddBff(this WebApplicationBuilder builder, IReadOnlyList<CultureInfo> cultures)
	{
		builder.Services.AddOptions<BffOptions>().BindConfiguration(BffOptions.SectionName).ValidateOnStart();
		builder.Services.AddSingleton<IValidateOptions<BffOptions>, BffOptionsValidator>();
		builder.Services.AddSingleton(new LoginLanguages(cultures));

		builder.Services.Configure<ClientConfiguration>(builder.Configuration.GetSection("Client"));

		builder.Services.ConfigureHttpJsonOptions(options =>
			options.SerializerOptions.TypeInfoResolverChain.Insert(0, BffJsonContext.Default));

		AddAuthentication(builder);
		FilterOtlpProxyFromTraces(builder);

		builder.Services.AddReverseProxy()
			.LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
			.AddServiceDiscoveryDestinationResolver()
			.AddCookieIsolationTransform()
			.AddAccessTokenTransform();

		return builder;
	}

	private static void AddAuthentication(WebApplicationBuilder builder)
	{
		builder.Services.AddOptions<OidcOptions>().BindConfiguration(OidcOptions.SectionName).ValidateOnStart();
		builder.Services.AddSingleton<IValidateOptions<OidcOptions>, OidcOptionsValidator>();

		builder.Services.AddAuthentication(options =>
			{
				options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
				options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
			})
			.AddCookie(options =>
			{
				// What the __Host- prefix of the session cookie's name requires (BffOptions.SessionCookie)
				options.Cookie.Path = "/";
				options.Cookie.SameSite = SameSiteMode.Lax;

				// Always use a Secure cookie, even when TLS is terminated at the ingress.
				options.Cookie.SecurePolicy = CookieSecurePolicy.Always;

				options.Events.OnRedirectToLogin = context =>
				{
					context.Response.StatusCode = StatusCodes.Status401Unauthorized;
					return Task.CompletedTask;
				};

				options.Events.OnRedirectToAccessDenied = context =>
				{
					context.Response.StatusCode = StatusCodes.Status403Forbidden;
					return Task.CompletedTask;
				};
			})
			.AddOpenIdConnect();

		builder.Services.AddOptions<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme)
			.Configure<IOptions<BffOptions>>((cookie, bff) => cookie.Cookie.Name = bff.Value.SessionCookie);

		builder.Services.AddOptions<OpenIdConnectOptions>(OpenIdConnectDefaults.AuthenticationScheme)
			.Configure<IOptions<OidcOptions>>((oidc, settings) =>
			{
				oidc.Authority = settings.Value.Authority;
				oidc.ClientId = settings.Value.ClientId;
				oidc.ClientSecret = settings.Value.ClientSecret;

				oidc.ResponseType = OpenIdConnectResponseType.Code;

				oidc.SaveTokens = true;
				oidc.GetClaimsFromUserInfoEndpoint = true;

				// Keep the original claim names
				oidc.MapInboundClaims = false;
				oidc.TokenValidationParameters.NameClaimType = JwtRegisteredClaimNames.PreferredUsername;

				// Both go into the pushed authorization request
				oidc.Events.OnRedirectToIdentityProvider = context =>
				{
					if (settings.Value.LoginHint is { } loginHint)
					{
						context.ProtocolMessage.LoginHint = loginHint;
					}

					if (context.Properties.Items.TryGetValue(AuthenticationEndpoints.UiLocales, out string? uiLocales))
					{
						context.ProtocolMessage.UiLocales = uiLocales;
					}

					return Task.CompletedTask;
				};
			});

		builder.Services.AddAuthorizationBuilder()
			.AddPolicy(
				ProxiedApiDefaults.Policy,
				policy => policy.AddAuthenticationSchemes(CookieAuthenticationDefaults.AuthenticationScheme)
					.RequireAuthenticatedUser()
					.RequireAssertion(context => context.Resource is HttpContext http &&
						http.Request.Headers.ContainsKey(ProxiedApiDefaults.CsrfHeader)));

		builder.Services.AddOpenIdConnectAccessTokenManagement();

		// /bff/user asks the API for the user's permissions, with the session's access token
		builder.Services.AddHttpClient<UsersClient>((provider, client) =>
				client.BaseAddress = new Uri(provider.GetRequiredService<IOptions<BffOptions>>().Value.Api))
			.AddUserAccessTokenHandler();
	}

	// YARP forwards /_otlp/* to the dashboard, and tracing those forwarding requests would export them again, in a loop
	private static void FilterOtlpProxyFromTraces(WebApplicationBuilder builder)
	{
		builder.Services.PostConfigure<AspNetCoreTraceInstrumentationOptions>(options =>
		{
			Func<HttpContext, bool>? previous = options.Filter;
			options.Filter = context =>
			{
				string? path = context.Request.Path.Value;
				return (previous is null || previous(context)) && (path is null || !path.Contains("/_otlp/", StringComparison.Ordinal));
			};
		});

		builder.Services.PostConfigure<HttpClientTraceInstrumentationOptions>(options =>
		{
			Func<HttpRequestMessage, bool>? previous = options.FilterHttpRequestMessage;
			options.FilterHttpRequestMessage = request =>
				(previous is null || previous(request)) &&
				(request.RequestUri is null || !request.RequestUri.AbsolutePath.StartsWith("/v1/", StringComparison.Ordinal));
		});
	}
}