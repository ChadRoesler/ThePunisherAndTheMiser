using Azure.Data.Tables;
using Azure.Identity;
using Graveyard.Models;
using Graveyard.Tables;
using Graveyard.Constants;
using Newtonsoft.Json;

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

        public async void WriteTagData(TagModel tagModel, int tagId)
        {
            if (tagModel.CurrentTags == null || tagModel.CurrentTags.Count == 0)
            {
                return;
            }
            var tagTable = new TagTable
            {
                PartitionKey = tagModel.ObjectType,
                RowKey = Guid.NewGuid().ToString(),
                ObjectId = tagModel.ObjectId,
                TagJson = JsonConvert.SerializeObject(tagModel.CurrentTags),
                Id = tagId
            };
            await _tagTableClient.AddEntityAsync(tagTable);
        }

        public async void WriteTagData(TagModel tagModel)
        {
            if (tagModel.CurrentTags == null || tagModel.CurrentTags.Count == 0)
            {
                return;
            }
            if (tagModel.TagHistory == null)
            {
                throw new ArgumentNullException(nameof(tagModel.TagHistory));
            }
            if (tagModel.TagHistory.Count == 0)
            {
                throw new Exception("Tag history is empty");
            }
            var tagId = (tagModel.TagHistory.Max(x => x.Id) + 1);
            var tagTable = new TagTable
            {
                PartitionKey = tagModel.ObjectType,
                RowKey = Guid.NewGuid().ToString(),
                ObjectId = tagModel.ObjectId,
                TagJson = JsonConvert.SerializeObject(tagModel.CurrentTags),
                Id = tagId
            };
            await _tagTableClient.AddEntityAsync(tagTable);
        }

        public async Task<int> GetNextId(string objectId, string objectType)
        {
            var tagTableList = new List<TagTable>();
            var tags = _tagTableClient.QueryAsync<TagTable>(x => x.PartitionKey == objectType && x.ObjectId == objectId);
            await foreach (var tag in tags)
            {
                tagTableList.Add(tag);
            }
            if (tagTableList.Count == 0)
            {
                return 1;
            }
            return tagTableList.Max(x => x.Id) + 1;
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
            foreach (var tag in tagTableList)
            {
                var historicTag = new HistoricTagModel
                {
                    Id = tag.Id,
                    Tags = !string.IsNullOrEmpty(tag.TagJson) ? JsonConvert.DeserializeObject<Dictionary<string, string>>(tag.TagJson) : new Dictionary<string, string>()
                };
                historicTagModels.Add(historicTag);
            }
            return historicTagModels;
        }
    }
}
