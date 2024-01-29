using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Internet_Engineering_Backend.Models;

public class ItemSharing
{
	[BsonId]
	[BsonRepresentation(BsonType.ObjectId)]
	public string Id { get; set; } = null!;
	public required string UserId { get; set; }
	public required string ItemId { get; set; }

	public DateTime ShareDate { get; set; } = DateTime.UtcNow;
}
