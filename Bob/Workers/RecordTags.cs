// Default URL for triggering event grid function in the local environment.
// http://localhost:7071/runtime/webhooks/EventGrid?functionName={functionname}

using Azure.Messaging;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Graveyard.Services;
using System.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Graveyard.ExtensionMethods;

namespace Bob.Workers
{
    public class RecordTags
    {
        private readonly ILogger<RecordTags> _logger;
        private readonly TableService _tableService;
        private readonly ResourceService _resourceService;

        public RecordTags(ILogger<RecordTags> logger)
        {
            var storageUri = Environment.GetEnvironmentVariable("StorageUri") ?? throw new ConfigurationErrorsException("StorageUri");
            _logger = logger;
            _tableService = new TableService(storageUri);
            _resourceService = new ResourceService();
        }

        [Function(nameof(RecordTags))]
        public async Task Run([EventGridTrigger] CloudEvent cloudEvent)
        {
            _logger.LogInformation("Event type: {type}, Event subject: {subject}", cloudEvent.Type, cloudEvent.Subject);
            if (cloudEvent.Type == "Microsoft.Resources.ResourceWriteSuccess")
            {
                var eventData = JsonConvert.DeserializeObject<JObject>(cloudEvent.Data.ToString());
                string resourceId = eventData["resourceUri"]?.ToString();
                if (string.IsNullOrEmpty(resourceId))
                {
                    _logger.LogError("Resource Id is missing in the event data.");
                    return;
                }

                var resource = await _resourceService.LoadResourceModel(resourceId);
                var tagModel = await _tableService.LoadTags(resourceId, resource.ResourceType);
                var hasTagHistory = tagModel != null && tagModel.Count > 0;
                var hasTags = resource.Tags?.CurrentTags != null && resource.Tags.CurrentTags.Count > 0;

                if (hasTagHistory && hasTags)
                {
                    var mostRecentTag = tagModel.OrderByDescending(x => x.Id).FirstOrDefault();
                    if (mostRecentTag != null && !mostRecentTag.Tags.IsEqualTo(resource.Tags.CurrentTags))
                    {
                        _tableService.WriteTagData(resource.Tags, mostRecentTag.Id);
                    }
                }
                else if (hasTags && !hasTagHistory)
                {
                    _tableService.WriteTagData(resource.Tags, 1);
                }
                else if (!hasTags && hasTagHistory)
                {
                    var maxTagId = tagModel.Max(x => x.Id);
                    _tableService.WriteTagData(resource.Tags, maxTagId);
                }
            }
        }
    }
}
