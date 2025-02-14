using Azure;
using Azure.Identity;
using Azure.ResourceManager;
using Frank.Constants;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Configuration;

namespace Frank.Workers
{
    public class Terminate
    {
        private readonly ILogger _logger;
        private readonly ArmClient _armClient;

        public Terminate(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<Terminate>();
            _armClient = new ArmClient(new DefaultAzureCredential());
        }

        [Function("Terminate")]
        public void Run([TimerTrigger("0 0 0 * * *")] TimerInfo myTimer)
        {
            _logger.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            var terminateKey = Environment.GetEnvironmentVariable(ResourceStrings.TerminateTagKey) ?? throw new ConfigurationErrorsException(ResourceStrings.TerminateTagKey);
            var terminateDefaultValueString = Environment.GetEnvironmentVariable(ResourceStrings.TerminateTagKeyDefault) ?? throw new ConfigurationErrorsException(ResourceStrings.TerminateTagKeyDefault);
            var immortalityValue = Environment.GetEnvironmentVariable(ResourceStrings.ImmortalSettingValue) ?? throw new ConfigurationErrorsException(ResourceStrings.ImmortalSettingValue);
            var dryRunFlag = Environment.GetEnvironmentVariable(ResourceStrings.DryRunFlag) ?? throw new ConfigurationErrorsException(ResourceStrings.DryRunFlag);
            if (!double.TryParse(terminateDefaultValueString, out double terminateDaysDefault))
            {
                terminateDaysDefault = 0.0;
            }

            foreach (var resourceGroup in _armClient.GetDefaultSubscription().GetResourceGroups())
            {
                foreach (var resource in resourceGroup.GetGenericResources())
                {
                    var termTagValue = resource.Data.Tags.FirstOrDefault(x => x.Key == terminateKey).Value ?? terminateDaysDefault.ToString();
                    if (string.Equals(termTagValue, immortalityValue, StringComparison.Ordinal))
                    {
                        return;
                    }
                    if (!double.TryParse(termTagValue, out var termTagValueDouble))
                    {
                        termTagValueDouble = terminateDaysDefault;
                    }
                    if (DateTimeOffset.Now.Subtract(resource.Data.CreatedOn ?? DateTimeOffset.Now).TotalDays < termTagValueDouble)
                    {
                        return;
                    }
                    resource.Delete(WaitUntil.Started);
                    _logger.LogInformation($"Deleted {resource.Data.ResourceType}: {resource.Id}");
                }
            }

            if (myTimer.ScheduleStatus is not null)
            {
                _logger.LogInformation($"Next timer schedule at: {myTimer.ScheduleStatus.Next}");
            }
        }
    }
}
