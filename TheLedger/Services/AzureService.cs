using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using TheLedger.Models;

namespace TheLedger.Services;

public class AzureService
{
    private readonly ArmClient _armClient;

    public AzureService()
    {
#if DEBUG
        _armClient = new ArmClient(new DefaultAzureCredential());
#else
        _armClient = new ArmClient(new InteractiveBrowserCredential());
#endif
    }

    public async Task<List<SubscriptionModel>> GetSubscriptionsAsync()
    {
        List<SubscriptionModel> subscriptionModels = new();
        await foreach (var sub in _armClient.GetSubscriptions().GetAllAsync())
        {
            subscriptionModels.Add(new SubscriptionModel
            {
                Id = sub.Data.Id,
                Name = sub.Data.DisplayName
            });
        }
        return subscriptionModels;
    }

    public async Task<List<ResourceGroupModel>> GetResourceGroupsAsync(string subscriptionId)
    {
        List<ResourceGroupModel> resourceGroupModels = new();
        var sub = _armClient.GetSubscriptionResource(new ResourceIdentifier($"/subscriptions/{subscriptionId}"));
        await foreach (var rg in sub.GetResourceGroups().GetAllAsync())
        {
            resourceGroupModels.Add(new ResourceGroupModel
            {
                Id = rg.Data.Id ?? string.Empty,
                Name = rg.Data.Name,
                Location = rg.Data.Location,
                Tags = rg.Data.Tags != null ? new Dictionary<string, string>(rg.Data.Tags) : null
            });
        }
        return resourceGroupModels;
    }

    public async Task<List<ResourceModel>> GetResourcesAsync(string resourceGroupId)
    {
        List<ResourceModel> resourceModels = new();
        var rg = _armClient.GetResourceGroupResource(new ResourceIdentifier(resourceGroupId));
        await foreach (var res in rg.GetGenericResourcesAsync())
        {
            resourceModels.Add(new ResourceModel
            {
                Id = res.Data.Id ?? string.Empty,
                Name = res.Data.Name,
                Location = res.Data.Location,
                Type = res.Data.ResourceType,
                Tags = res.Data.Tags != null ? new Dictionary<string, string>(res.Data.Tags) : null
            });
        }
        return resourceModels;
    }
}
