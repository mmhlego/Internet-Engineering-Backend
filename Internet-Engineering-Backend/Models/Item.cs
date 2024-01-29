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
	public string IconColor { get; set; } = "#ffffff";
	public required ItemTypes ItemType { get; set; }
	public bool IsFavorite { get; set; } = false;
}

public enum ItemTypes
{
	Folder,
	Audio,
	Video,
	Image,
	Document,
	Others,
}

public class File : Item
{
	public bool IsEncrypted { get; set; } = true;
	public required long ContentSize { get; set; }
	public required string ObjectName { get; set; }
}

public class Folder : Item
{
	public required int Depth { get; set; }
	public SortOrders SortOrder { get; set; } = SortOrders.Name;
}

public enum SortOrders
{
	Name,
	CreationDate,
	ModificationDate,
	ChangeVisibility,
}