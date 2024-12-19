using Azure;
using Azure.Identity;
using Azure.ResourceManager;
using Ebenezer.Constants;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Configuration;

namespace Ebenezer.Workers.Tagging
{
    /// <summary>
    /// Azure Function to add Startup tags to resources.
    /// </summary>
    public class StartupTagging
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="StartupTagging"/> class.
        /// </summary>
        /// <param name="loggerFactory">The logger factory to create loggers.</param>
        public StartupTagging(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<StartupTagging>();
        }

        /// <summary>
        /// Function to add startup tags to resources.
        /// </summary>
        /// <param name="myTimer">Timer trigger information.</param>
        [Function("StartupTagging")]
        public async Task RunAsync([TimerTrigger("0 0 1 * * *")] TimerInfo myTimer)
        {
            _logger.LogInformation($"C# StartupTagging function executed at: {DateTime.Now}");
            var credential = new DefaultAzureCredential();
            var armClient = new ArmClient(credential);
            var startupTagKey = Environment.GetEnvironmentVariable(ResourceStrings.StartupTagKey) ?? throw new ConfigurationErrorsException(ResourceStrings.StartupTagKey);
            var startupTagKeyDefault = Environment.GetEnvironmentVariable(ResourceStrings.StartupTagKeyDefault) ?? throw new ConfigurationErrorsException(ResourceStrings.StartupTagKeyDefault);
            var startupTimeTagKey = Environment.GetEnvironmentVariable(ResourceStrings.StartupTimeTagKey) ?? throw new ConfigurationErrorsException(ResourceStrings.StartupTimeTagKey);
            var startupTimeTagKeyDefault = Environment.GetEnvironmentVariable(ResourceStrings.StartupTimeTagKeyDefault) ?? throw new ConfigurationErrorsException(ResourceStrings.StartupTimeTagKeyDefault);
            var startupTagsDict = new Dictionary<string, string>
            {
                { startupTagKey, startupTagKeyDefault },
                { startupTimeTagKey, startupTimeTagKeyDefault }
            };

            var resourceGroups = armClient.GetDefaultSubscription().GetResourceGroups();
            await foreach (var resourceGroup in resourceGroups)
            {
                var resources = resourceGroup.GetGenericResourcesAsync();
                await foreach (var resource in resources)
                {
                    foreach (var tagKey in startupTagsDict.Keys)
                    {
                        if (resource.Data.Tags.ContainsKey(tagKey))
                        {
                            return;
                        }
                        try
                        {
                            resource.AddTag(tagKey, startupTagsDict[tagKey]);
                            _logger.LogInformation($"Added Tag: {tagKey} to {resource.Data.ResourceType}: {resource.Id}");
                        }
                        catch (RequestFailedException ex)
                        {
                            _logger.LogError($"Error adding Tag: {tagKey} to {resource.Data.ResourceType}: {resource.Id}");
                            _logger.LogError($"Error: {ex.Message}");
                        }
                    }
                }
            }
            if (myTimer.ScheduleStatus is not null)
            {
                _logger.LogInformation($"Next timer schedule at: {myTimer.ScheduleStatus.Next}");
            }
        }
    }
}
