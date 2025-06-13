using RoutingService;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddHostedService<RoutingWorker>();

var host = builder.Build();
host.Run();
