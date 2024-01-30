namespace Internet_Engineering_Backend.Models;

public class StorageUsageResponse
{
	public required long TotalSpace { get; set; }
	public required long TotalUsage { get; set; }
}

public class UserResponse
{
	public required string Id { get; set; }
	public required string Username { get; set; }
	public required string FirstName { get; set; }
	public required string LastName { get; set; }
	public required string EmailAddress { get; set; }
	public required bool Restricted { get; set; }
}

public class ProfileResponse
{
	public required string Username { get; set; }
	public required string FirstName { get; set; }
	public required string LastName { get; set; }
	public required string EmailAddress { get; set; }
	public required string Role { get; set; }
}

public class CredentialsResponse
{
	public required string IV { get; set; }
	public required string Key { get; set; }
}

public class ItemResponse
{
	public required string Id { get; set; }
	public required string Name { get; set; }
	public required string ParentId { get; set; }
	public required DateTime CreationDate { get; set; }
	public required string IconColor { get; set; }
	public required string ItemType { get; set; }
	public required bool IsFavorite { get; set; }
	public required long Size { get; set; }
	public required bool IsShared { get; set; }
}

public class ItemInfoResponse : ItemResponse
{
	public required List<string> Tags { get; set; }
	public required string Description { get; set; }
	public required List<ItemActivity> Activities { get; set; }
}

public class FolderContentResponse : Pagination<ItemResponse>
{
	public required string SortOrder { get; set; }
	public required bool SortAscending { get; set; }
}

public class ItemActivity
{
	public required DateTime Date { get; set; }
	public required string Operation { get; set; }
}

public class DownloadResponse
{
	public required string Url { get; set; }
	public required string Filename { get; set; }
	public required bool IsEncrypted { get; set; }
}
