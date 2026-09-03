using Argus.Data;
using Argus.Worker;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

builder.AddNpgsqlDbContext<AppDbContext>("argusdb");

builder.Services.Configure<ChainIngestOptions>(builder.Configuration.GetSection(ChainIngestOptions.SectionName));
builder.Services.AddHostedService<EvmDeploymentListener>();

builder.Build().Run();