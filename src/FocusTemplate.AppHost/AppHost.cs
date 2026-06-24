IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.FocusTemplate_Web>("web");

builder.Build().Run();