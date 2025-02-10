using Azure;
using Azure.Data.Tables;

namespace Graveyard.Tables
{
    public class TagTable : ITableEntity
    {
        public required string RowKey { get; set; } = Guid.NewGuid().ToString();

        public required string PartitionKey { get; set; }

        public required string ObjectId { get; set; }

        public required string TagJson { get; set; }
        public required int Id { get; set; }

        public ETag ETag { get; set; } = ETag.All;

        public DateTimeOffset? Timestamp { get; set; }
    }
}
