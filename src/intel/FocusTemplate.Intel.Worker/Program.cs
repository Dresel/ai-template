using FocusTemplate.Data;
using FocusTemplate.Intel.Worker;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

builder.AddNpgsqlDbContext<AppDbContext>("focusdb");

builder.Services.Configure<ChainIngestOptions>(builder.Configuration.GetSection(ChainIngestOptions.SectionName));
builder.Services.AddHostedService<EvmDeploymentListener>();

builder.Build().Run();