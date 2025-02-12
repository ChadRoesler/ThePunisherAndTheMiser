// Default URL for triggering event grid function in the local environment.
// http://localhost:7071/runtime/webhooks/EventGrid?functionName={functionname}

using Azure.Messaging;
using Graveyard.ExtensionMethods;
using Graveyard.Models;
using Graveyard.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Configuration;

namespace Bob.Workers
{
    public class RecordTags
    {
        private readonly ILogger<RecordTags> _logger;
        private readonly TableService _tableService;
        private readonly ResourceService _resourceService;

        public RecordTags(ILogger<RecordTags> logger, TableService tableService, ResourceService resource)
        {
            _logger = logger;
            _logger.LogInformation("RecordTags function is starting.");
            _logger.LogDebug("Environment variables: {0}", JsonConvert.SerializeObject(Environment.GetEnvironmentVariables()));
            _logger.LogDebug("Configuration: {0}", JsonConvert.SerializeObject(ConfigurationManager.AppSettings));
            _logger.LogInformation("Gathering EnvironmentVariables...");
            
            _logger.LogInformation("Initializing services...");
            _tableService = tableService;
            _resourceService = resource;
        }

        [Function(nameof(RecordTags))]
        public async Task Run([EventGridTrigger] CloudEvent cloudEvent)
        {
            _logger.LogInformation("Event type: {type}, Event subject: {subject}", cloudEvent.Type, cloudEvent.Subject);
            if (cloudEvent.Type == "Microsoft.Resources.ResourceWriteSuccess")
            {
                var eventData = JsonConvert.DeserializeObject<JObject>(cloudEvent.Data.ToString());
                string? resourceId = eventData["resourceUri"]?.ToString();
                if (string.IsNullOrEmpty(resourceId))
                {
                    _logger.LogError("Resource Id is missing in the event data.");
                    return;
                }

                var resource = _resourceService.LoadResource(resourceId);
                var tagModel = await _tableService.LoadTagHistoryAsync(resourceId, resource.ResourceType);
                var hasTagHistory = tagModel != null && tagModel.Count > 0;
                var hasTags = resource.Tags?.CurrentTags != null && resource.Tags.CurrentTags.Count > 0;

                if (hasTagHistory && hasTags)
                {
                    var mostRecentTag = tagModel.OrderByDescending(x => x.Id).FirstOrDefault();
                    if (mostRecentTag != null && !mostRecentTag.Tags.IsEqualTo(resource.Tags.CurrentTags))
                    {
                        await _tableService.WriteTagDataAsync(resource.Tags, mostRecentTag.Id);
                    }
                }
                else if (hasTags && !hasTagHistory)
                {
                    await _tableService.WriteTagDataAsync(resource.Tags, 1);
                }
                else if (!hasTags && hasTagHistory)
                {
                    var maxTagId = tagModel.Max(x => x.Id);
                    await _tableService.WriteTagDataAsync(resource.Tags ?? new TagModel(), maxTagId);
                }
            }
        }
    }
}
