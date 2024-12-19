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
    public class Shutdown
    {
        private readonly ILogger _logger;

        public Shutdown(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<Shutdown>();
        }

        [Function("Shutdown")]
        public void Run([TimerTrigger("0 0 1 * * *")] TimerInfo myTimer)
        {
            _logger.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            var credential = new DefaultAzureCredential();
            var armClient = new ArmClient(credential);
            var shutdownTagKey = Environment.GetEnvironmentVariable(ResourceStrings.ShutdownTagKey) ?? throw new ConfigurationErrorsException(ResourceStrings.ShutdownTagKey);
            var shutdownTagKeyDefault = Environment.GetEnvironmentVariable(ResourceStrings.ShutdownTagKeyDefault) ?? throw new ConfigurationErrorsException(ResourceStrings.ShutdownTagKeyDefault);
            var shutdownTimeTagKey = Environment.GetEnvironmentVariable(ResourceStrings.ShutdownTimeTagKey) ?? throw new ConfigurationErrorsException(ResourceStrings.ShutdownTimeTagKey);
            var shutdownTimeTagKeyDefaultString = Environment.GetEnvironmentVariable(ResourceStrings.ShutdownTimeTagKeyDefault) ?? throw new ConfigurationErrorsException(ResourceStrings.ShutdownTimeTagKeyDefault);

            if (!int.TryParse(shutdownTimeTagKeyDefaultString, out int shutdownTimeTagKeyDefault))
            {
                shutdownTimeTagKeyDefault = 0;
            }

            var currentHour = DateTime.Now.Hour;
            var shutdownTimeInt = 0;

            foreach (var resourceGroup in armClient.GetDefaultSubscription().GetResourceGroups())
            {
                foreach (var virtualMachine in resourceGroup.GetVirtualMachines())
                {
                    var shutdown = virtualMachine.Data.Tags.FirstOrDefault(x => x.Key == shutdownTimeTagKey, new KeyValuePair<string, string>(shutdownTagKey, ResourceStrings.ShutdownTagKeyDefault)).Value;
                    var shutdownTime = virtualMachine.Data.Tags.FirstOrDefault(x => x.Key == shutdownTimeTagKey, new KeyValuePair<string, string>(shutdownTimeTagKey, ResourceStrings.ShutdownTimeTagKeyDefault)).Value;
                    if (!int.TryParse(shutdownTime, out shutdownTimeInt))
                    {
                        _logger.LogError($"Failed to parse shutdown time: {shutdownTime} for VM: {virtualMachine.Data.Name}");
                        continue;
                    }
                    shutdownTimeInt = (shutdownTimeInt > 24 ? shutdownTimeTagKeyDefault : shutdownTimeInt) < 0 ? shutdownTimeTagKeyDefault : shutdownTimeInt;
                    if (!string.Equals(shutdown, true.ToString().ToLower()) || currentHour != shutdownTimeInt)
                    {
                        return;
                    }
                    virtualMachine.PowerOff(WaitUntil.Started);
                }
            }

            if (myTimer.ScheduleStatus is not null)
            {
                _logger.LogInformation($"Next timer schedule at: {myTimer.ScheduleStatus.Next}");
            }
        }
    }
}
