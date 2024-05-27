using BlogManagement.Models;
using MongoDB.Driver;

namespace BlogManagement.DBContext
{
    public class MongoDbContext
    {
        private readonly IMongoDatabase _database;

        public MongoDbContext(string connectionString, string databaseName)
        {
            var client = new MongoClient(connectionString);
            _database = client.GetDatabase(databaseName);
        }

        public IMongoCollection<LogEntry> LogEntries => _database.GetCollection<LogEntry>("LogEntries");
    }
}
