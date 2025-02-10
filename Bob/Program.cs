using Graveyard.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Configuration;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
        services.AddSingleton(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<TableService>>();
            var storageUri = Environment.GetEnvironmentVariable("StorageUri") ?? throw new ConfigurationErrorsException("StorageUri");
            return new TableService(storageUri, logger);
        });
        services.AddSingleton(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<ResourceService>>();
            return new ResourceService(logger);
        });
    })
    .Build();

host.Run();