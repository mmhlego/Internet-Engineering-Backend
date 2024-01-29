using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Internet_Engineering_Backend.Models;

public class Activity
{
	[BsonId]
	[BsonRepresentation(BsonType.ObjectId)]
	public string Id { get; set; } = null!;
	public required string UserId { get; set; }
	public required string ItemId { get; set; }

	public required bool IsFolder { get; set; }
	public DateTime ModificationDate { get; set; } = DateTime.UtcNow;
	public required Operations Operation { get; set; }
}

public enum Operations
{
	Create,
	Modify,
	GenerateShare,
}
