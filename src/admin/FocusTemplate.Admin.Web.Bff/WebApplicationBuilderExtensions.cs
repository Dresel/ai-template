using Duende.AccessTokenManagement.OpenIdConnect;
using FocusTemplate.Admin.Client.Users;
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
				// __Host-: browsers require Secure, Path=/ and no Domain, so no subdomain can overwrite the session.
				options.Cookie.Name = "__Host-focus.session";
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
		builder.Services.AddHttpClient<UsersClient>(client => client.BaseAddress = new Uri("https+http://admin-api/"))
			.AddUserAccessTokenHandler();

		return builder;
	}
}