using Azure.Core;
using Azure.Data.Tables;
using Azure.Identity;
using Graveyard.Constants;
using Graveyard.Models;
using Graveyard.Tables;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Graveyard.Services
{
    public class TagTableService
    {
        private readonly TableServiceClient _tableServiceClient;
        private readonly TableClient _tagTableClient;
        private readonly ILogger<TagTableService> _logger;

        public TagTableService(string tableUri, ILogger<TagTableService> logger)
        {
            _tableServiceClient = new TableServiceClient(new Uri(tableUri), new DefaultAzureCredential());
            _tagTableClient = _tableServiceClient.GetTableClient(ResourceStrings.TagHistoryTableName);
            _logger = logger;
        }

        public TagTableService(string tableUri, TokenCredential tokenCredential, ILogger<TagTableService> logger)
        {
            _tableServiceClient = new TableServiceClient(new Uri(tableUri), tokenCredential);
            _tagTableClient = _tableServiceClient.GetTableClient(ResourceStrings.TagHistoryTableName);
            _logger = logger;
        }

        public async Task WriteTagDataAsync(TagModel tagModel, int tagId)
        {
            _logger.LogInformation("Writing tag data for ObjectId: {ObjectId}, TagId: {TagId}", tagModel.ObjectId, tagId);
            var tagTable = new TagTable
            {
                PartitionKey = tagModel.ObjectType.Replace('/', '.').Replace('/', '.').Replace('#', '.').Replace('?', '.'),
                RowKey = Guid.NewGuid().ToString(),
                ObjectId = tagModel.ObjectId,
                TagJson = JsonConvert.SerializeObject(tagModel.CurrentTags),
                Id = tagId
            };
            await _tagTableClient.AddEntityAsync(tagTable);
            _logger.LogInformation("Successfully wrote tag data for ObjectId: {ObjectId}, TagId: {TagId}", tagModel.ObjectId, tagId);
        }

        public async Task WriteTagDataAsync(TagModel tagModel)
        {
            _logger.LogInformation("Writing tag data for ObjectId: {ObjectId}", tagModel.ObjectId);
            var tagId = tagModel.TagHistory.Max(x => x.Id) + 1;
            var tagTable = new TagTable
            {
                PartitionKey = tagModel.ObjectType.Replace('/', '.').Replace('/', '.').Replace('#', '.').Replace('?', '.'),
                RowKey = Guid.NewGuid().ToString(),
                ObjectId = tagModel.ObjectId,
                TagJson = JsonConvert.SerializeObject(tagModel.CurrentTags),
                Id = tagId
            };
            await _tagTableClient.AddEntityAsync(tagTable);
            _logger.LogInformation("Successfully wrote tag data for ObjectId: {ObjectId}, TagId: {TagId}", tagModel.ObjectId, tagId);
        }

        public async Task<int> GetNextIdAsync(string objectId, string objectType)
        {
            _logger.LogInformation("Getting next ID for ObjectId: {ObjectId}, ObjectType: {ObjectType}", objectId, objectType);
            var tagTableList = new List<TagTable>();
            var tags = _tagTableClient.QueryAsync<TagTable>(x => x.PartitionKey == objectType && x.ObjectId == objectId);
            await foreach (var tag in tags)
            {
                tagTableList.Add(tag);
            }
            var nextId = tagTableList.Count == 0 ? 1 : tagTableList.Max(x => x.Id) + 1;
            _logger.LogInformation("Next ID for ObjectId: {ObjectId}, ObjectType: {ObjectType} is {NextId}", objectId, objectType, nextId);
            return nextId;
        }

        public async Task<List<HistoricTagModel>> LoadTagHistoryAsync(string objectId, string objectType)
        {
            _logger.LogInformation("Loading tags for ObjectId: {ObjectId}, ObjectType: {ObjectType}", objectId, objectType);
            var transformedObjectType = objectType.Replace('/','.').Replace('/','.').Replace('#', '.').Replace('?', '.');
            var historicTagModels = new List<HistoricTagModel>();
            var tagTableList = new List<TagTable>();
            var tags = _tagTableClient.QueryAsync<TagTable>(x => x.PartitionKey == transformedObjectType && x.ObjectId == objectId);
            await foreach (var tag in tags)
            {
                tagTableList.Add(tag);
            }
            if (tagTableList.Count == 0)
            {
                _logger.LogInformation("No tags found for ObjectId: {ObjectId}, ObjectType: {ObjectType}", objectId, objectType);
                return historicTagModels;
            }
            foreach (var tag in tagTableList)
            {
                var historicTag = new HistoricTagModel
                {
                    Id = tag.Id,
                    Tags = !string.IsNullOrEmpty(tag.TagJson) ? JsonConvert.DeserializeObject<Dictionary<string, string>>(tag.TagJson) : [],
                    Timestamp = tag.Timestamp,
                    ObjectType = objectType,
                    ObjectId = objectId
                };
                historicTagModels.Add(historicTag);
            }
            _logger.LogInformation("Loaded {Count} tags for ObjectId: {ObjectId}, ObjectType: {ObjectType}", historicTagModels.Count, objectId, objectType);
            return historicTagModels;
        }
    }
}
