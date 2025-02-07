using Azure.Identity;
using Azure.Core;
using Azure.ResourceManager;
using Graveyard.Models;
using Graveyard.ExtensionMethods;

namespace Graveyard.Services
{
    public class ResourceService
    {
        private static ArmClient _armClient;

        public ResourceService()
        {
            _armClient = new ArmClient(new DefaultAzureCredential());
        }

        public async Task<List<SubscriptionModel>> LoadSubscriptions()
        {
            var subscriptionObjs = new List<SubscriptionModel>();
            await foreach (var sub in _armClient.GetSubscriptions().GetAllAsync())
            {
                var subscription = new SubscriptionModel
                {
                    Id = sub.Data.Id,
                    Name = sub.Data.DisplayName
                };
                subscriptionObjs.Add(subscription);
            }
            return subscriptionObjs;
        }

        public async Task<List<ResourceGroupModel>> LoadResourceGroups(string subscriptionId)
        {
            var resourceGroupObjs = new List<ResourceGroupModel>();
            var sub = _armClient.GetSubscriptionResource(new ResourceIdentifier($"/subscriptions/{subscriptionId}"));
            await foreach (var rg in sub.GetResourceGroups().GetAllAsync())
            {
                var resourceGroup = new ResourceGroupModel
                {
                    Id = rg.Data.Id ?? string.Empty,
                    Name = rg.Data.Name,
                    Location = rg.Data.Location.ToString(),
                    SubscriptionId = subscriptionId
                };
                resourceGroup.Tags = new TagModel
                {
                    ObjectId = resourceGroup.Id,
                    ObjectType = "ResourceGroup",
                    CurrentTags = new Dictionary<string, string>(),
                    TagHistory = new List<HistoricTagModel>()
                };
                resourceGroup.Tags.CurrentTags = rg.Data.VisibleTags();
                resourceGroupObjs.Add(resourceGroup);
            }
            return resourceGroupObjs;
        }

        public async Task<ResourceModel> LoadResourceModel(string resourceId)
        {
            var resource = new ResourceModel();
            var res = _armClient.GetGenericResource(new ResourceIdentifier(resourceId));
            var resourceGroupId = string.Empty;
            string[] parts = resourceId.ToString().Split('/');
            int rgIndex = Array.IndexOf(parts, "resourceGroups");
            if (rgIndex != -1 && rgIndex + 1 < parts.Length)
            {
                resourceGroupId = parts[rgIndex + 1]; // Resource group name is the next segment after "resourceGroups"
            }
            resource.ResourceGroupId = resourceGroupId;
            resource.Id = resourceId;
            resource.Name = res.Data.Name;
            resource.Location = res.Data.Location.ToString();
            resource.Tags = new TagModel
            {
                ObjectId = resource.Id,
                ObjectType = "Resource",
                CurrentTags = new Dictionary<string, string>(),
                TagHistory = new List<HistoricTagModel>()
            };
            resource.Tags.CurrentTags = res.Data.VisibleTags();
            return resource;
        }

        public async Task<List<ResourceModel>> LoadResources(string resourceGroupId)
        {
            var resourceObjs = new List<ResourceModel>();
            var rg = _armClient.GetResourceGroupResource(new ResourceIdentifier(resourceGroupId));
            await foreach (var res in rg.GetGenericResourcesAsync())
            {
                var resource = new ResourceModel
                {
                    Id = res.Data.Id ?? string.Empty,
                    Name = res.Data.Name,
                    Location = res.Data.Location.ToString(),
                    ResourceGroupId = resourceGroupId
                };
                resource.Tags = new TagModel
                {
                    ObjectId = resource.Id,
                    ObjectType = "Resource",
                    CurrentTags = new Dictionary<string, string>(),
                    TagHistory = new List<HistoricTagModel>()
                };
                resource.Tags.CurrentTags = res.Data.VisibleTags();
                resourceObjs.Add(resource);
            }
            return resourceObjs;
        }
    }
}
