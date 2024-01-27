using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Internet_Engineering_Backend.Models;

public class Item
{
	[BsonId]
	[BsonRepresentation(BsonType.ObjectId)]
	public string Id { get; set; } = null!;
	public required string OwnerId { get; set; }
	public required string ParentId { get; set; }

	public required string Name { get; set; }
	public DateTime CreationDate { get; set; } = DateTime.UtcNow;
	public List<string> Tags { get; set; } = new List<string>();
	public string Description { get; set; } = "";
	public bool IsEncrypted { get; set; } = true;
	public string IconColor { get; set; } = "#ffffff";
}

public class File : Item
{
	public required long ContentSize { get; set; }
	public required string FileName { get; set; }
	public required string ContentUrl { get; set; }
	public required FileTypes FileType { get; set; }
}

public enum FileTypes
{
	Audio,
	Video,
	Image,
	Document,
	Others,
}

public class Folder : Item
{
	public SortOrders SortOrder { get; set; } = SortOrders.Name;
}

public enum SortOrders
{
	Name,
	CreationDate,
	ModificationDate,
}