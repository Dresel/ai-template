using Duende.AccessTokenManagement.OpenIdConnect;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace FocusTemplate.Admin.Web.Bff;

public static class WebApplicationBuilderExtensions
{
	public static WebApplicationBuilder AddBffAuthentication(this WebApplicationBuilder builder)
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
				options.Cookie.Name = "focus.session";
				options.Cookie.SameSite = SameSiteMode.Lax;

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

		builder.Services.AddOptions<OpenIdConnectOptions>(OpenIdConnectDefaults.AuthenticationScheme)
			.Configure<IOptions<OidcOptions>>((oidc, settings) =>
			{
				oidc.Authority = settings.Value.Authority;
				oidc.ClientId = settings.Value.ClientId;
				oidc.ClientSecret = settings.Value.ClientSecret;

				oidc.ResponseType = OpenIdConnectResponseType.Code;

				// TLS ends at the ingress
				oidc.ResponseMode = OpenIdConnectResponseMode.Query;
				oidc.CorrelationCookie.SameSite = SameSiteMode.Lax;
				oidc.NonceCookie.SameSite = SameSiteMode.Lax;

				oidc.SaveTokens = true;
				oidc.GetClaimsFromUserInfoEndpoint = true;

				// Keep the original claim names
				oidc.MapInboundClaims = false;
				oidc.TokenValidationParameters.NameClaimType = JwtRegisteredClaimNames.PreferredUsername;
			});

		builder.Services.AddAuthorizationBuilder()
			.AddPolicy(
				ProxiedApiDefaults.Policy,
				policy => policy.AddAuthenticationSchemes(CookieAuthenticationDefaults.AuthenticationScheme)
					.RequireAuthenticatedUser()
					.RequireAssertion(context => context.Resource is HttpContext http &&
						http.Request.Headers.ContainsKey(ProxiedApiDefaults.CsrfHeader)));

		builder.Services.AddOpenIdConnectAccessTokenManagement();

		return builder;
	}
}