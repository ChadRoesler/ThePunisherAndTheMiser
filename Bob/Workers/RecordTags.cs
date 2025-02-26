using Azure.Messaging;
using BobCratchit.Constants;
using Graveyard.ExtensionMethods;
using Graveyard.Models;
using Graveyard.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Configuration;

namespace BobCratchit.Workers
{
    /// <summary>
    /// Azure Function to record tags for resources.
    /// </summary>
    public class RecordTags
    {
        private readonly ILogger<RecordTags> _logger;
        private readonly TagTableService _tableService;
        private readonly ResourceService _resourceService;
        private readonly string[] _resourceTypes;

        /// <summary>
        /// Initializes a new instance of the <see cref="RecordTags"/> class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="tableService">The tag table service instance.</param>
        /// <param name="resourceService">The resource service instance.</param>
        public RecordTags(ILogger<RecordTags> logger, TagTableService tableService, ResourceService resourceService)
        {
            _logger = logger;
            _logger.LogInformation("RecordTags function is starting.");
            _logger.LogDebug("Environment variables: {environmentVarsJson}", JsonConvert.SerializeObject(Environment.GetEnvironmentVariables()));
            _logger.LogDebug("Configuration: {appSettingsJson}", JsonConvert.SerializeObject(ConfigurationManager.AppSettings));
            _logger.LogInformation("Gathering EnvironmentVariables...");
            _resourceTypes = Environment.GetEnvironmentVariable(ResourceStrings.ResourceTypes)?.Split(',') ?? throw new ConfigurationErrorsException(ResourceStrings.ResourceTypes);
            _logger.LogInformation("Initializing services...");
            _tableService = tableService;
            _resourceService = resourceService;
        }

        /// <summary>
        /// Azure Function entry point to process Event Grid events.
        /// </summary>
        /// <param name="cloudEvent">The cloud event received from Event Grid.</param>
        [Function(nameof(RecordTags))]
        public async Task Run([EventGridTrigger] CloudEvent cloudEvent)
        {
            TagModel resourceTagObject = new();

            _logger.LogDebug("Event received: {cloudEvent}", JsonConvert.SerializeObject(cloudEvent));
            _logger.LogInformation("Event type: {type}, Event subject: {subject}", cloudEvent.Type, cloudEvent.Subject);
            if (cloudEvent.Type != "Microsoft.Resources.ResourceWriteSuccess")
            {
                _logger.LogInformation("Event type is not a resource write success event. Skipping...");
                return;
            }
            _logger.LogDebug("Event data: {data}", JsonConvert.SerializeObject(cloudEvent.Data));
            if (cloudEvent.Data == null || string.IsNullOrEmpty(cloudEvent.Data.ToString()))
            {
                _logger.LogError("Event data is null.");
                return;
            }
            var eventData = JsonConvert.DeserializeObject<JObject>(cloudEvent.Data.ToString());
            string? resourceId = eventData[ResourceStrings.ResourceUri]?.ToString();
            if (string.IsNullOrEmpty(resourceId))
            {
                _logger.LogError("Resource Id is missing in the event data.");
                return;
            }
            _logger.LogInformation("Resource ID: {resourceId}", resourceId);

            string resourceType = ResourceService.GetResourceType(resourceId);
            if (string.IsNullOrEmpty(resourceType))
            {
                _logger.LogError("Resource type is missing in the event data.");
                return;
            }
            _logger.LogInformation("Resource type: {resourceType}", resourceType);
            if (!_resourceTypes.Contains("all") && !_resourceTypes.Contains(resourceType))
            {
                _logger.LogInformation("Resource type is not in the list of resource types to record tags for. Skipping...");
                return;
            }
            switch (resourceType)
            {
                case "Microsoft.Resources/resourceGroups":
                    _logger.LogInformation("Loading resource group tags for resource ID: {resourceId}", resourceId);
                    var resourceGroup = _resourceService.LoadResourceGroup(resourceId);
                    resourceTagObject = resourceGroup.Tags;
                    break;
                default:
                    _logger.LogInformation("Loading resource tags for resource ID: {resourceId}", resourceId);
                    var resource = _resourceService.LoadResource(resourceId);
                    resourceTagObject = resource.Tags;
                    break;
            }
            _logger.LogInformation("Loading tag history for resource ID: {resourceId}, resource type: {resourceType}", resourceId, resourceType);
            var tagModel = await _tableService.LoadTagHistoryAsync(resourceId, resourceType);
            var hasTagHistory = tagModel != null && tagModel.Count > 0;
            var hasTags = resourceTagObject.CurrentTags.Count > 0;

            _logger.LogInformation("Has tag history: {hasTagHistory}, Has tags: {hasTags}", hasTagHistory, hasTags);

            if (hasTagHistory && hasTags)
            {
                var mostRecentTag = tagModel.OrderByDescending(x => x.Id).FirstOrDefault();
                if (mostRecentTag != null && !mostRecentTag.Tags.IsEqualTo(resourceTagObject.CurrentTags))
                {
                    _logger.LogInformation("Tag changes detected. Writing new tag data.");
                    await _tableService.WriteTagDataAsync(resourceTagObject, mostRecentTag.Id + 1);
                }
                else
                {
                    _logger.LogInformation("No tag changes detected.");
                }
            }
            else if (hasTags && !hasTagHistory)
            {
                _logger.LogInformation("No tag history found. Writing initial tag data.");
                await _tableService.WriteTagDataAsync(resourceTagObject, 1);
            }
            else if (!hasTags && hasTagHistory)
            {
                _logger.LogInformation("Tags removed. Writing new tag data with incremented ID.");
                var maxTagId = tagModel.Max(x => x.Id);
                await _tableService.WriteTagDataAsync(resourceTagObject, maxTagId + 1);
            }
            else
            {
                _logger.LogInformation("No tags and no tag history found. No action taken.");
            }
        }
    }
}