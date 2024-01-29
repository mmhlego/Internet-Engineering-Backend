using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Internet_Engineering_Backend.Models;

public class User
{
	[BsonId]
	[BsonRepresentation(BsonType.ObjectId)]
	public string Id { get; set; } = null!;

	public required string Username { get; set; }
	public required string Password { get; set; }
	public required string Salt { get; set; }
	public required string FirstName { get; set; }
	public required string LastName { get; set; }
	public required string EmailAddress { get; set; }
	public UserRoles Role { get; set; } = UserRoles.Basic;
	public bool IsRestricted { get; set; } = false;

	// Protected with password
	public required string EncryptionKey { get; set; }
}

public enum UserRoles
{
	Admin,
	Basic,
}
