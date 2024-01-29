using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Internet_Engineering_Backend.Data;

public class CustomLink
{
	[BsonId]
	[BsonRepresentation(BsonType.ObjectId)]
	public string Id { get; set; } = null!;
	public required string ItemId { get; set; }

	public required int Usage { get; set; }
	public required DateTime ExpiryDate { get; set; }
}
