using Azure;
using Azure.Identity;
using Azure.ResourceManager;
using Ebenezer.Constants;
using Microsoft.Azure.Functions.Worker;
using System.Configuration;
using Microsoft.Extensions.Logging;


namespace Ebenezer.Workers.Tagging
{
    /// <summary>
    /// Azure Function to add shutdown tags to resources.
    /// </summary>
    public class ShutdownTagging
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ShutdownTagging"/> class.
        /// </summary>
        /// <param name="loggerFactory">The logger factory to create loggers.</param>
        public ShutdownTagging(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<ShutdownTagging>();
        }

        // ... other parts of the class ...

        [Function("ShutdownTagging")]
        public async Task Run([TimerTrigger("0 0 1 * * *")] TimerInfo myTimer)
        {
            _logger.LogInformation($"C# ShutdownTagging function executed at: {DateTime.Now}");
            var credential = new DefaultAzureCredential();
            var armClient = new ArmClient(credential);
            var shutdownTagKey = Environment.GetEnvironmentVariable(ResourceStrings.ShutdownTagKey) ?? throw new ConfigurationErrorsException(ResourceStrings.ShutdownTagKey);
            var shutdownTagKeyDefault = Environment.GetEnvironmentVariable(ResourceStrings.ShutdownTagKeyDefault) ?? throw new ConfigurationErrorsException(ResourceStrings.ShutdownTagKeyDefault);
            var shutdownTimeTagKey = Environment.GetEnvironmentVariable(ResourceStrings.ShutdownTimeTagKey) ?? throw new ConfigurationErrorsException(ResourceStrings.ShutdownTimeTagKey);
            var shutdownTimeTagKeyDefault = Environment.GetEnvironmentVariable(ResourceStrings.ShutdownTimeTagKeyDefault) ?? throw new ConfigurationErrorsException(ResourceStrings.ShutdownTagKeyDefault);

            var shutdownTagsDict = new Dictionary<string, string>
            {
                { shutdownTagKey, shutdownTagKeyDefault },
                { shutdownTimeTagKey, shutdownTimeTagKeyDefault }
            };

            foreach (var resourceGroup in armClient.GetDefaultSubscription().GetResourceGroups())
            {
                var resources = resourceGroup.GetGenericResourcesAsync();
                await foreach (var resource in resources)
                {
                    foreach (var tagKey in shutdownTagsDict.Keys)
                    {
                        if (resource.Data.Tags.ContainsKey(tagKey))
                        {
                            return;
                        }
                        try
                        {
                            await resource.AddTagAsync(tagKey, shutdownTagsDict[tagKey]);
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
