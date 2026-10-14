using FocusTemplate.Admin.Shared.Localization;
using FocusTemplate.Admin.Web.Bff;
using FocusTemplate.Admin.Web.Bff.Diagnostics;
using SpatialFocus.Admin.UserManagement.Client.Users;
using SpatialFocus.Web.Bff.Hosting;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddBff(Cultures.Supported);
builder.Services.AddApiClient<UsersClient>();
builder.Services.AddScoped<IUserPermissionSource, UserPermissionSource>();

builder.Services.ConfigureHttpJsonOptions(options =>
	options.SerializerOptions.TypeInfoResolverChain.Insert(0, DiagnosticsJsonContext.Default));

WebApplication app = builder.Build();

app.MapDefaultEndpoints();

app.UseAuthentication();
app.UseAuthorization();

app.MapBffEndpoints();

if (app.Environment.IsDevelopment())
{
	app.MapDebugEndpoint();
}

// The client this BFF serves, whose files arrive through its project reference
app.MapStaticAssets();
app.MapFallbackToFile("index.html");

app.Run();