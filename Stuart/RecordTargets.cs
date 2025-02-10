using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Stuart
{
    public class RecordTargets
    {
        private readonly ILogger _logger;

        public RecordTargets(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<RecordTargets>();
        }

        [Function("Function1")]
        public void Run([TimerTrigger("0 0 * * * *")] TimerInfo myTimer)
        {
            _logger.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            if (myTimer.ScheduleStatus is not null)
            {
                _logger.LogInformation($"Next timer schedule at: {myTimer.ScheduleStatus.Next}");
            }
        }
    }
}
