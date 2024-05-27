using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace BlogManagement.Models
{
    public class LogEntry
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string Level { get; set; }
        public string Message { get; set; }
        public string Exception { get; set; }
        public string User { get; set; }
        public string Source { get; set; }
        public string AdditionalInfo { get; set; }
    }
}
