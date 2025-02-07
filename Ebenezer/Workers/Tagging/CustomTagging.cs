using Azure;
using Azure.Identity;
using Azure.ResourceManager;
using Ebenezer.Constants;
using Graveyard.ExtensionMethods;
using Microsoft.Azure.Functions.Worker;
using System.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;


namespace Ebenezer.Workers.Tagging
{
    public class CustomTagging
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomTagging"/> class.
        /// </summary>
        /// <param name="loggerFactory">The logger factory to create loggers.</param>
        public CustomTagging(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<CustomTagging>();
        }

        [Function("CustomTagging")]
        public async Task Run([TimerTrigger("0 0 1 * * *")] TimerInfo myTimer)
        {
            _logger.LogInformation($"C# CustomTagging function executed at: {DateTime.Now}");
            var credential = new DefaultAzureCredential();
            var armClient = new ArmClient(credential);
            var startupTagKey = Environment.GetEnvironmentVariable(ResourceStrings.CustomTagsJson) ?? throw new ConfigurationErrorsException(ResourceStrings.CustomTagsJson);
            var customKeyDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(startupTagKey) ?? [];
            foreach (var resourceGroup in armClient.GetDefaultSubscription().GetResourceGroups())
            {
                var resources = resourceGroup.GetGenericResourcesAsync();
                await foreach (var resource in resources)
                {

                    if(customKeyDict.Equals(resource.Data.Tags))
                    {
                        return;
                    }
                    foreach (var tag in customKeyDict)
                    {
                        if (resource.Data.Tags.ContainsKey(tag.Key))
                        {
                            return;
                        }
                        try
                        {
                            resource.Ae(tag.Key, tag.Value);
                            _logger.LogInformation($"Added Tag: {tag.Key} to {resource.Data.ResourceType}: {resource.Id}");
                        }
                        catch (RequestFailedException ex)
                        {
                            _logger.LogError($"Error adding Tag: {tag.Key} to {resource.Data.ResourceType}: {resource.Id}");
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
