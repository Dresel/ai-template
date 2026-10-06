using FocusTemplate.Admin.Api.Authorization;
using FocusTemplate.Data.Auditing;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;

namespace FocusTemplate.Admin.Api.Authentication;

public static class WebApplicationBuilderExtensions
{
	public static WebApplicationBuilder AddApiAuthentication(this WebApplicationBuilder builder)
	{
		builder.Services.AddOptions<OidcOptions>().BindConfiguration(OidcOptions.SectionName).ValidateOnStart();
		builder.Services.AddSingleton<IValidateOptions<OidcOptions>, OidcOptionsValidator>();

		builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
		builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
			.Configure<IOptions<OidcOptions>>((jwt, oidc) =>
			{
				jwt.Authority = oidc.Value.Authority;
				jwt.Audience = oidc.Value.Audience;

				// Keep the original claim names
				jwt.MapInboundClaims = false;
				jwt.TokenValidationParameters.NameClaimType = JwtRegisteredClaimNames.PreferredUsername;

				jwt.Events.OnTokenValidated = context =>
				{
					if (context.Principal?.UserIdOrDefault is null)
					{
						context.Fail("The token's sub claim is no UUID.");
					}

					return Task.CompletedTask;
				};
			});

		builder.Services.AddAuthorization();
		builder.Services.AddScoped<UserPermissions>();
		builder.Services.AddScoped<IAuthorizationHandler, PermissionRequirementHandler>();

		builder.Services.AddMemoryCache();

		builder.Services.AddHttpContextAccessor();
		builder.Services.AddSingleton<ICurrentUser, HttpContextCurrentUser>();

		return builder;
	}
}