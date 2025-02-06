using Azure.Data.Tables;
using Azure.Identity;
using Graveyard.Models;
using Graveyard.Tables;
using Graveyard.Constants;

namespace Graveyard.Services
{
    public class TableService
    {
        private static string _tableUri = string.Empty;
        private TableServiceClient _tableServiceClient;
        private TableClient _tagTableClient;

        public TableService(string tableUri)
        {
            _tableUri = tableUri;
            _tableServiceClient = new TableServiceClient(new Uri(_tableUri), new DefaultAzureCredential());
            _tagTableClient = _tableServiceClient.GetTableClient(ResourceStrings.TagHistoryTableName);
        }

        public async void WriteTagData(TagModel tagModel)
        {
            foreach (var tag in tagModel.CurrentTags)
            {
                var tagTable = new TagTable
                {
                    PartitionKey = tagModel.ObjectType,
                    RowKey = Guid.NewGuid().ToString(),
                    ObjectId = tagModel.ObjectId,
                    TagKey = tag.Key,
                    TagValue = tag.Value
                };
                await _tagTableClient.AddEntityAsync(tagTable);
            }
        }

        public async Task<List<HistoricTagModel>> LoadTags(string objectId, string objectType)
        {
            var historicTagModels = new List<HistoricTagModel>();
            var tagTableList = new List<TagTable>();
            var tags = _tagTableClient.QueryAsync<TagTable>(x => x.PartitionKey == objectType && x.ObjectId == objectId);
            await foreach (var tag in tags)
            {
                tagTableList.Add(tag);
            }
            if (tagTableList.Count == 0)
            {
                return historicTagModels;
            }
            foreach(var tagId in tagTableList.Select(x => x.Id).Distinct().OrderDescending())
            {
                var historicTag = new HistoricTagModel
                {
                    Id = tagId,
                    Tags = tagTableList.Where(x => x.Id == tagId).Select(x => new KeyValuePair<string, string>(x.TagKey, x.TagValue)).ToDictionary(x => x.Key, x => x.Value)
                };
                historicTagModels.Add(historicTag);
            }
            return historicTagModels;
        }
    }
}
