using Internet_Engineering_Backend.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Internet_Engineering_Backend.Data;

public class DbContext : IDisposable
{
	public IMongoCollection<User> Users;
	public IMongoCollection<Models.File> Files;
	public IMongoCollection<Folder> Folders;
	public IMongoCollection<Activity> Activities;
	public IMongoCollection<SystemSettings> SystemSettings;

	public DbContext(IOptions<DatabaseSettings> settings)
	{
		var mongoClient = new MongoClient(settings.Value.ConnectionString);
		var mongoDatabase = mongoClient.GetDatabase(settings.Value.DatabaseName);

		// Add collections
		Users = mongoDatabase.GetCollection<User>("Users");
		Files = mongoDatabase.GetCollection<Models.File>("Files");
		Folders = mongoDatabase.GetCollection<Folder>("Folders");
		Activities = mongoDatabase.GetCollection<Activity>("Activities");
		SystemSettings = mongoDatabase.GetCollection<SystemSettings>("SystemSettings");
	}

	public void Dispose() { }
}
