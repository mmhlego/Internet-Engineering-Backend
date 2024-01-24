using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Internet_Engineering_Backend.Models;

public class Item
{
	[BsonId]
	[BsonRepresentation(BsonType.ObjectId)]
	public string Id { get; set; } = null!;
	public required string OwnerId { get; set; }
	public string? ParentId { get; set; } = null;

	public required string Name { get; set; }
	public DateTime CreationDate { get; set; } = DateTime.UtcNow;
	public List<string> Tags { get; set; } = new List<string>();
	public string Description { get; set; } = "";
	public bool IsEncrypted { get; set; } = true;
}

public class File : Item
{
	public required long ContentSize { get; set; }
	public required string ContentUrl { get; set; }
}

public class Folder : Item
{
	public string Color { get; set; } = "#ffffff";
	public SortOrders SortOrder { get; set; } = SortOrders.Name;
}

public enum SortOrders
{
	Name,
	CreationDate,
	ModificationDate,
}