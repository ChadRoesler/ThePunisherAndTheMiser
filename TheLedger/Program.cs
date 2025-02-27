using Graveyard.Services;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using TheLedger;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");

builder.Services.AddScoped<ResourceService>();

// Configure Logging
builder.Logging.SetMinimumLevel(LogLevel.Debug); // Set the logging level (Debug, Info, Warning, Error)

#if DEBUG
builder.Services.AddMsalAuthentication(options =>
{
    builder.Configuration.Bind("AzureAd", options.ProviderOptions.Authentication);
});
#else
builder.Services.AddMsalAuthentication(options =>
{
    builder.Configuration.Bind("AzureAd", options.ProviderOptions.Authentication);
    options.ProviderOptions.DefaultAccessTokenScopes.Add("https://management.azure.com/user_impersonation");
});
#endif

await builder.Build().RunAsync();
