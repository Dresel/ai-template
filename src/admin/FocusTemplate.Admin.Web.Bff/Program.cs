using FocusTemplate.Admin.Shared.Localization;
using FocusTemplate.Web.Bff.Hosting;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddBff(Cultures.Supported);

WebApplication app = builder.Build();

app.MapDefaultEndpoints();

app.UseAuthentication();
app.UseAuthorization();

app.MapBffEndpoints();

// The client this BFF serves, whose files arrive through its project reference
app.MapStaticAssets();
app.MapFallbackToFile("index.html");

app.Run();