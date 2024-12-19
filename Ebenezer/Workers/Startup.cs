using Azure;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Compute;
using Ebenezer.Constants;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Configuration;


namespace Ebenezer.Workers
{
    public class Startup
    {
        private readonly ILogger _logger;

        public Startup(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<Startup>();
        }

        [Function("Startup")]
        public void Run([TimerTrigger("0 0 1 * * *")] TimerInfo myTimer)
        {
            _logger.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            var credential = new DefaultAzureCredential();
            var armClient = new ArmClient(credential);
            var startupTagKey = Environment.GetEnvironmentVariable(ResourceStrings.StartupTagKey) ?? throw new ConfigurationErrorsException(ResourceStrings.StartupTagKey);
            var startupTagKeyDefault = Environment.GetEnvironmentVariable(ResourceStrings.StartupTagKeyDefault) ?? throw new ConfigurationErrorsException(ResourceStrings.StartupTagKeyDefault);
            var startupTimeTagKey = Environment.GetEnvironmentVariable(ResourceStrings.StartupTimeTagKey) ?? throw new ConfigurationErrorsException(ResourceStrings.StartupTimeTagKey);
            var startupTimeTagKeyDefaultString = Environment.GetEnvironmentVariable(ResourceStrings.StartupTimeTagKeyDefault) ?? throw new ConfigurationErrorsException(ResourceStrings.StartupTimeTagKeyDefault);

            if (!int.TryParse(startupTimeTagKeyDefaultString, out int startupTimeTagKeyDefault))
            {
                startupTimeTagKeyDefault = 0;
            }
            var currentHour = DateTime.Now.Hour;
            var startupTimeInt = 0;

            foreach (var resourceGroup in armClient.GetDefaultSubscription().GetResourceGroups())
            {
                foreach (var virtualMachine in resourceGroup.GetVirtualMachines())
                {
                    var startup = virtualMachine.Data.Tags.FirstOrDefault(x => x.Key == startupTagKey, new KeyValuePair<string, string>(startupTagKey, ResourceStrings.StartupTagKeyDefault)).Value;
                    var startupTime = virtualMachine.Data.Tags.FirstOrDefault(x => x.Key == startupTimeTagKey, new KeyValuePair<string, string>(startupTimeTagKey, ResourceStrings.StartupTimeTagKeyDefault)).Value;
                    if (!int.TryParse(startupTime, out startupTimeInt))
                    {
                        startupTimeInt = startupTimeTagKeyDefault;
                    }
                    startupTimeInt = (startupTimeInt > 24 ? startupTimeTagKeyDefault : startupTimeInt) < 0 ? startupTimeTagKeyDefault : startupTimeInt;
                    if (!string.Equals(startup, true.ToString().ToLower()) || currentHour != startupTimeInt)
                    {
                        return;
                    }
                    virtualMachine.PowerOn(WaitUntil.Started);
                }
            }

            if (myTimer.ScheduleStatus is not null)
            {
                _logger.LogInformation($"Next timer schedule at: {myTimer.ScheduleStatus.Next}");
            }
        }
    }
}
