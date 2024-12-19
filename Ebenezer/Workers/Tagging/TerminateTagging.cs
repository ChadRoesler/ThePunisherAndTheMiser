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
    /// Azure Function to add Terminate tags to resources.
    /// </summary>
    public class TerminateTagging
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="TerminateTagging"/> class.
        /// </summary>
        /// <param name="loggerFactory">The logger factory to create loggers.</param>
        public TerminateTagging(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<TerminateTagging>();
        }

        /// <summary>
        /// Function to add terminate tags to resources.
        /// </summary>
        /// <param name="myTimer">Timer trigger information.</param>
        [Function("TerminateTagging")]
        public void Run([TimerTrigger("0 0 1 * * *")] TimerInfo myTimer)
        {
            _logger.LogInformation($"C# TerminateTagging function executed at: {DateTime.Now}");
            var credential = new DefaultAzureCredential();
            var armClient = new ArmClient(credential);
            var terminateTagKey = Environment.GetEnvironmentVariable(ResourceStrings.TerminateTagKey) ?? throw new ConfigurationErrorsException(ResourceStrings.TerminateTagKey);
            var terminateTagKeyDefault = Environment.GetEnvironmentVariable(ResourceStrings.TerminateTagKeyDefault) ?? throw new ConfigurationErrorsException(ResourceStrings.TerminateTagKeyDefault);

            foreach (var resourceGroup in armClient.GetDefaultSubscription().GetResourceGroups())
            {
                foreach (var resource in resourceGroup.GetGenericResources())
                {
                    if (resource.Data.Tags.ContainsKey(terminateTagKey))
                    {
                        return;
                    }
                    try
                    {
                        resource.AddTag(terminateTagKey, terminateTagKeyDefault);
                        resource.Update(WaitUntil.Started, resource.Data);
                        _logger.LogInformation($"Added Tag: {terminateTagKey} to {resource.Data.ResourceType}: {resource.Id}");
                    }
                    catch (RequestFailedException ex)
                    {
                        _logger.LogError($"Error adding Tag: {terminateTagKey} to {resource.Data.ResourceType}: {resource.Id}");
                        _logger.LogError($"Error: {ex.Message}");
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
