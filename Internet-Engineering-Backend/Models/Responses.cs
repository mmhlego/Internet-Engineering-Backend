namespace Internet_Engineering_Backend.Models;

public class StorageUsageResponse
{
	public required long TotalSpace { get; set; }
	public required long TotalUsage { get; set; }
}

public class UserResponse
{
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
