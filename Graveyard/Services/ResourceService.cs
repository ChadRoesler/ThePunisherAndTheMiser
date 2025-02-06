using Azure.Data.Tables;
using Azure.Identity;
using Azure.Core;
using Azure.ResourceManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Graveyard.Models;

namespace Graveyard.Services
{
    internal class ResourceService
    {
        private static string _tableUri = string.Empty;
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
                    SubscriptionId = subscriptionId
                };
                resourceGroup.Tags = new TagModel
                {
                    ObjectId = resourceGroup.Id,
                    ObjectType = "ResourceGroup",
                    CurrentTags = new Dictionary<string, string>(),
                    TagHistory = new List<HistoricTagModel>()
                };
                if (rg.Data.Tags != null)
                {
                    resourceGroup.Tags.CurrentTags = new Dictionary<string, string>(rg.Data.Tags);
                }
                resourceGroupObjs.Add(resourceGroup);
            }
            return resourceGroupObjs;
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
                if (res.Data.Tags != null)
                {
                    resource.Tags.CurrentTags = new Dictionary<string, string>(res.Data.Tags);
                }
                resourceObjs.Add(resource);
            }
            return resourceObjs;
        }
    }
}
