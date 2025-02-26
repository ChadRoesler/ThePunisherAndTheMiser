using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Graveyard.Constants;
using Graveyard.ExtensionMethods;
using Graveyard.Models;
using Microsoft.Extensions.Logging;

namespace Graveyard.Services
{
    /// <summary>
    /// Service for managing Azure resources.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="ResourceService"/> class using the specified token credential.
    /// </remarks>
    /// <param name="tokenCredential">The token credential.</param>
    /// <param name="logger">The logger instance.</param>
    public class ResourceService(TokenCredential tokenCredential, ILogger<ResourceService> logger)
    {
        private readonly ArmClient _armClient = new(tokenCredential);
        private readonly ILogger<ResourceService> _logger = logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceService"/> class using the default Azure credential.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        public ResourceService(ILogger<ResourceService> logger)
            : this(new DefaultAzureCredential(), logger)
        {
        }

        /// <summary>
        /// Gets the resource type from the specified resource ID.
        /// </summary>
        /// <param name="resourceId">The resource ID.</param>
        /// <returns>The resource type.</returns>
        /// <exception cref="ArgumentException">Thrown when the resource ID is invalid.</exception>
        public static string GetResourceType(string resourceId)
        {
            var resourceIdObj = new ResourceIdentifier(resourceId);
            return resourceIdObj?.ResourceType.Type ?? throw new ArgumentException("Invalid resource ID.");
        }

        /// <summary>
        /// Extracts the resource group ID from the specified resource ID.
        /// </summary>
        /// <param name="resourceId">The resource ID.</param>
        /// <returns>The resource group ID.</returns>
        private static string GetResourceGroupIdFromResourceId(string resourceId)
        {
            var parts = resourceId.Split('/');
            var rgIndex = Array.IndexOf(parts, ResourceStrings.ResourceGroupIndex);
            return rgIndex != -1 && rgIndex + 1 < parts.Length ? parts[rgIndex + 1] : string.Empty;
        }

        /// <summary>
        /// Loads the resource group with the specified ID.
        /// </summary>
        /// <param name="resourceGroupId">The resource group ID.</param>
        /// <returns>The resource group model.</returns>
        public ResourceGroupModel LoadResourceGroup(string resourceGroupId)
        {
            _logger.LogInformation("Loading resource group with ID: {ResourceGroupId}", resourceGroupId);
            var rg = _armClient.GetResourceGroupResource(new ResourceIdentifier(resourceGroupId));
            var resourceGroup = new ResourceGroupModel
            {
                Id = rg.Data.Id ?? string.Empty,
                Name = rg.Data.Name ?? string.Empty,
                Location = rg.Data.Location.ToString() ?? string.Empty,
                SubscriptionId = rg.Id.SubscriptionId ?? string.Empty,
                Tags = new TagModel
                {
                    ObjectId = resourceGroupId,
                    ObjectType = ResourceStrings.ResourceGroupTypeName,
                    CurrentTags = rg.Data.VisibleTags() ?? [],
                    TagHistory = []
                }
            };
            _logger.LogInformation("Loaded resource group: {ResourceGroupName}", resourceGroup.Name);
            return resourceGroup;
        }

        /// <summary>
        /// Loads the resource with the specified ID.
        /// </summary>
        /// <param name="resourceId">The resource ID.</param>
        /// <returns>The resource model.</returns>
        public ResourceModel LoadResource(string resourceId)
        {
            _logger.LogInformation("Loading resource with ID: {ResourceId}", resourceId);
            var res = _armClient.GetGenericResource(new ResourceIdentifier(resourceId));
            var resourceGroupId = GetResourceGroupIdFromResourceId(resourceId);
            var resource = new ResourceModel
            {
                Id = resourceId,
                Name = res.Data.Name ?? string.Empty,
                Location = res.Data.Location.ToString() ?? string.Empty,
                ResourceGroupId = resourceGroupId,
                Tags = new TagModel
                {
                    ObjectId = resourceId,
                    ObjectType = res.Data.ResourceType,
                    CurrentTags = res.Data.VisibleTags() ?? [],
                    TagHistory = []
                }
            };
            _logger.LogInformation("Loaded resource: {ResourceName}", resource.Name);
            return resource;
        }

        /// <summary>
        /// Loads all subscriptions.
        /// </summary>
        /// <returns>A list of subscription models.</returns>
        public async Task<List<SubscriptionModel>> LoadSubscriptions()
        {
            _logger.LogInformation("Loading subscriptions...");
            var subscriptionObjs = new List<SubscriptionModel>();
            await foreach (var sub in _armClient.GetSubscriptions().GetAllAsync())
            {
                subscriptionObjs.Add(new SubscriptionModel
                {
                    Id = sub.Data.Id ?? string.Empty,
                    Name = sub.Data.DisplayName ?? string.Empty
                });
            }
            _logger.LogInformation("Loaded {Count} subscriptions.", subscriptionObjs.Count);
            return subscriptionObjs;
        }

        /// <summary>
        /// Loads all resource groups for the specified subscription ID.
        /// </summary>
        /// <param name="subscriptionId">The subscription ID.</param>
        /// <returns>A list of resource group models.</returns>
        public async Task<List<ResourceGroupModel>> LoadResourceGroups(string subscriptionId)
        {
            _logger.LogInformation("Loading resource groups for subscription ID: {SubscriptionId}", subscriptionId);
            var resourceGroupObjs = new List<ResourceGroupModel>();
            var sub = _armClient.GetSubscriptionResource(new ResourceIdentifier(string.Format(ResourceStrings.SubscriptionResourceIdentifier, subscriptionId)));
            await foreach (var rg in sub.GetResourceGroups().GetAllAsync())
            {
                resourceGroupObjs.Add(new ResourceGroupModel
                {
                    Id = rg.Data.Id ?? string.Empty,
                    Name = rg.Data.Name ?? string.Empty,
                    Location = rg.Data.Location.ToString() ?? string.Empty,
                    SubscriptionId = subscriptionId,
                    Tags = new TagModel
                    {
                        ObjectId = rg.Data.Id ?? string.Empty,
                        ObjectType = ResourceStrings.ResourceGroupTypeName,
                        CurrentTags = rg.Data.VisibleTags() ?? [],
                        TagHistory = []
                    }
                });
            }
            _logger.LogInformation("Loaded {Count} resource groups for subscription ID: {SubscriptionId}", resourceGroupObjs.Count, subscriptionId);
            return resourceGroupObjs;
        }

        /// <summary>
        /// Loads all resources for the specified resource group ID.
        /// </summary>
        /// <param name="resourceGroupId">The resource group ID.</param>
        /// <returns>A list of resource models.</returns>
        public async Task<List<ResourceModel>> LoadResources(string resourceGroupId)
        {
            _logger.LogInformation("Loading resources for resource group ID: {ResourceGroupId}", resourceGroupId);
            var resourceObjs = new List<ResourceModel>();
            var rg = _armClient.GetResourceGroupResource(new ResourceIdentifier(resourceGroupId));
            await foreach (var res in rg.GetGenericResourcesAsync())
            {
                resourceObjs.Add(new ResourceModel
                {
                    Id = res.Data.Id ?? string.Empty,
                    Name = res.Data.Name ?? string.Empty,
                    Location = res.Data.Location.ToString() ?? string.Empty,
                    ResourceGroupId = resourceGroupId,
                    Tags = new TagModel
                    {
                        ObjectId = res.Data.Id ?? string.Empty,
                        ObjectType = res.Data.ResourceType,
                        CurrentTags = res.Data.VisibleTags() ?? [],
                        TagHistory = []
                    }
                });
            }
            _logger.LogInformation("Loaded {Count} resources for resource group ID: {ResourceGroupId}", resourceObjs.Count, resourceGroupId);
            return resourceObjs;
        }

        /// <summary>
        /// Reverts the tags for the specified resource group.
        /// </summary>
        /// <param name="resourceGroupId">The resource group ID.</param>
        /// <param name="tagsToRevert">The tags to revert.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task RevertResourceGroupTags(string resourceGroupId, Dictionary<string, string> tagsToRevert)
        {
            var rg = _armClient.GetResourceGroupResource(new ResourceIdentifier(resourceGroupId));
            var tags = rg.Data.VisibleTags();
            _logger.LogInformation("Reverting tags for resource group: {ResourceGroupId}", resourceGroupId);
            tags.Merge(tagsToRevert, true);
            await rg.SetTagsAsync(tags);
            _logger.LogInformation("Successfully reverted tags for resource group: {ResourceGroupId}", resourceGroupId);
        }

        /// <summary>
        /// Reverts the tags for the specified resource.
        /// </summary>
        /// <param name="resourceId">The resource ID.</param>
        /// <param name="tagsToRevert">The tags to revert.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task RevertResourceTags(string resourceId, Dictionary<string, string> tagsToRevert)
        {
            var res = _armClient.GetGenericResource(new ResourceIdentifier(resourceId));
            var tags = res.Data.VisibleTags();
            _logger.LogInformation("Reverting tags for resource: {ResourceId}", resourceId);
            tags.Merge(tagsToRevert, true);
            await res.SetTagsAsync(tags);
            _logger.LogInformation("Successfully reverted tags for resource: {ResourceId}", resourceId);
        }
    }
}
