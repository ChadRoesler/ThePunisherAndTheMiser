using Azure.Identity;
using Azure.ResourceManager;
using Ebenezer.Constants;
using Graveyard.ExtensionMethods;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Configuration;


namespace Ebenezer.Workers.Tagging
{
    public class CustomTagging
    {
        private readonly ILogger _logger;
        private readonly ArmClient _armClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomTagging"/> class.
        /// </summary>
        /// <param name="loggerFactory">The logger factory to create loggers.</param>
        public CustomTagging(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<CustomTagging>();
            _armClient = new ArmClient(new DefaultAzureCredential());
        }

        [Function("CustomTagging")]
        public async Task Run([TimerTrigger("0 0 1 * * *")] TimerInfo myTimer)
        {
            _logger.LogInformation($"C# CustomTagging function executed at: {DateTime.Now}");
            var startupTagKey = Environment.GetEnvironmentVariable(ResourceStrings.CustomTagsJson) ?? throw new ConfigurationErrorsException(ResourceStrings.CustomTagsJson);
            var customKeyDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(startupTagKey) ?? [];
            foreach (var resourceGroup in _armClient.GetDefaultSubscription().GetResourceGroups())
            {
                var resources = resourceGroup.GetGenericResourcesAsync();
                await foreach (var resource in resources)
                {
                    var resourceTags = resource.Data.VisibleTags();
                    if (resourceTags.IsEqualTo(customKeyDict))
                    {
                        return;
                    }
                    else
                    {
                        resourceTags.Merge(customKeyDict);
                        resource.SetTags(resourceTags);
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
