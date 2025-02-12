using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Graveyard.Constants;
using Graveyard.ExtensionMethods;
using Graveyard.Models;
using Microsoft.Extensions.Logging;

namespace Graveyard.Services
{
    public class ResourceService
    {
        private readonly ArmClient _armClient;
        private readonly ILogger<ResourceService> _logger;

        public ResourceService(ILogger<ResourceService> logger)
        {
            _armClient = new ArmClient(new DefaultAzureCredential());
            _logger = logger;
        }

        public ResourceService(TokenCredential tokenCredential, ILogger<ResourceService> logger)
        {
            _armClient = new ArmClient(tokenCredential);
            _logger = logger;
        }

        public async Task<List<SubscriptionModel>> LoadSubscriptions()
        {
            _logger.LogInformation("Loading subscriptions...");
            var subscriptionObjs = new List<SubscriptionModel>();
            await foreach (var sub in _armClient.GetSubscriptions().GetAllAsync())
            {
                var subscription = new SubscriptionModel
                {
                    Id = sub.Data.Id ?? string.Empty,
                    Name = sub.Data.DisplayName ?? string.Empty
                };
                subscriptionObjs.Add(subscription);
            }
            _logger.LogInformation("Loaded {Count} subscriptions.", subscriptionObjs.Count);
            return subscriptionObjs;
        }

        public ResourceGroupModel LoadResourceGroup(string resourceGroupId)
        {
            _logger.LogInformation("Loading resource group with ID: {ResourceGroupId}", resourceGroupId);
            var resourceGroup = new ResourceGroupModel();
            var rg = _armClient.GetResourceGroupResource(new ResourceIdentifier(resourceGroupId));
            resourceGroup.Id = rg.Data.Id ?? string.Empty;
            resourceGroup.Name = rg.Data.Name ?? string.Empty;
            resourceGroup.Location = rg.Data.Location.ToString() ?? string.Empty;
            resourceGroup.SubscriptionId = rg.Id.SubscriptionId ?? string.Empty;
            resourceGroup.Tags = new TagModel
            {
                ObjectId = resourceGroupId,
                ObjectType = ResourceStrings.ResourceGroupTypeName,
                CurrentTags = rg.Data.VisibleTags() ?? new Dictionary<string, string>(),
                TagHistory = new List<HistoricTagModel>()
            };
            _logger.LogInformation("Loaded resource group: {ResourceGroupName}", resourceGroup.Name);
            return resourceGroup;
        }

        public async Task<List<ResourceGroupModel>> LoadResourceGroups(string subscriptionId)
        {
            _logger.LogInformation("Loading resource groups for subscription ID: {SubscriptionId}", subscriptionId);
            var resourceGroupObjs = new List<ResourceGroupModel>();
            var sub = _armClient.GetSubscriptionResource(new ResourceIdentifier(string.Format(ResourceStrings.SubscriptionResourceIdentifier, subscriptionId)));
            await foreach (var rg in sub.GetResourceGroups().GetAllAsync())
            {
                var resourceGroup = new ResourceGroupModel
                {
                    Id = rg.Data.Id ?? string.Empty,
                    Name = rg.Data.Name ?? string.Empty,
                    Location = rg.Data.Location.ToString() ?? string.Empty,
                    SubscriptionId = subscriptionId,
                    Tags = new TagModel
                    {
                        ObjectId = rg.Data.Id ?? string.Empty,
                        ObjectType = ResourceStrings.ResourceGroupTypeName,
                        CurrentTags = rg.Data.VisibleTags() ?? new Dictionary<string, string>(),
                        TagHistory = new List<HistoricTagModel>()
                    }
                };
                resourceGroupObjs.Add(resourceGroup);
            }
            _logger.LogInformation("Loaded {Count} resource groups for subscription ID: {SubscriptionId}", resourceGroupObjs.Count, subscriptionId);
            return resourceGroupObjs;
        }

        public ResourceModel LoadResource(string resourceId)
        {
            _logger.LogInformation("Loading resource with ID: {ResourceId}", resourceId);
            var resource = new ResourceModel();
            var res = _armClient.GetGenericResource(new ResourceIdentifier(resourceId));
            var resourceGroupId = string.Empty;
            string[] parts = resourceId.ToString().Split('/');
            int rgIndex = Array.IndexOf(parts, ResourceStrings.ResourceGroupIndex);
            if (rgIndex != -1 && rgIndex + 1 < parts.Length)
            {
                resourceGroupId = parts[rgIndex + 1]; // Resource group name is the next segment after "resourceGroups"
            }
            resource.ResourceGroupId = resourceGroupId;
            resource.Id = resourceId;
            resource.Name = res.Data.Name ?? string.Empty;
            resource.Location = res.Data.Location.ToString() ?? string.Empty;
            resource.Tags = new TagModel
            {
                ObjectId = resource.Id,
                ObjectType = res.Data.ResourceType,
                CurrentTags = res.Data.VisibleTags() ?? new Dictionary<string, string>(),
                TagHistory = new List<HistoricTagModel>()
            };
            _logger.LogInformation("Loaded resource: {ResourceName}", resource.Name);
            return resource;
        }

        public async Task<List<ResourceModel>> LoadResources(string resourceGroupId)
        {
            _logger.LogInformation("Loading resources for resource group ID: {ResourceGroupId}", resourceGroupId);
            var resourceObjs = new List<ResourceModel>();
            var rg = _armClient.GetResourceGroupResource(new ResourceIdentifier(resourceGroupId));
            await foreach (var res in rg.GetGenericResourcesAsync())
            {
                var resource = new ResourceModel
                {
                    Id = res.Data.Id ?? string.Empty,
                    Name = res.Data.Name ?? string.Empty,
                    Location = res.Data.Location.ToString() ?? string.Empty,
                    ResourceGroupId = resourceGroupId,
                    Tags = new TagModel
                    {
                        ObjectId = res.Data.Id ?? string.Empty,
                        ObjectType = res.Data.ResourceType,
                        CurrentTags = res.Data.VisibleTags() ?? new Dictionary<string, string>(),
                        TagHistory = new List<HistoricTagModel>()
                    }
                };
                resourceObjs.Add(resource);
            }
            _logger.LogInformation("Loaded {Count} resources for resource group ID: {ResourceGroupId}", resourceObjs.Count, resourceGroupId);
            return resourceObjs;
        }

        public async Task RevertResourceGroupTags(string resourceGroupId, Dictionary<string, string> tagsToRevert)
        {
            var rg = _armClient.GetResourceGroupResource(new ResourceIdentifier(resourceGroupId));
            var tags = rg.Data.VisibleTags();
            _logger.LogInformation("Reverting tags for resource group: {ResourceGroupId}", resourceGroupId);
            tags.Merge(tagsToRevert, true);
            await rg.SetTagsAsync(tags);
            _logger.LogInformation("Successfully reverted tags for resource group: {ResourceGroupId}", resourceGroupId);

        }
        public async Task RevertResourceTags(string resourceId, Dictionary<string, string> tagsToRevert)
        {
            var res = _armClient.GetGenericResource(new ResourceIdentifier(resourceId));
            var tags = res.Data.VisibleTags();
            _logger.LogInformation("Reverting tags for resource: {ResourceId}", resourceId);
            tags.Merge(tagsToRevert, true);
            await res.SetTagsAsync(tags);
            _logger.LogInformation("Successfully reverted tags for resource group: {ObjectId}", resourceId);
        }
    }
}
