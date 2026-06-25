IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.FocusTemplate_Api>("focustemplate-api");
builder.AddProject<Projects.FocusTemplate_Web>("web");

builder.Build().Run();