namespace Internet_Engineering_Backend.Models;

public class LoginRequest
{
	public required string Username { get; set; }
	public required string Password { get; set; }
}

public class RegisterRequest
{
	public required string Username { get; set; }
	public required string Password { get; set; }
	public required string FirstName { get; set; }
	public required string LastName { get; set; }
	public required string EmailAddress { get; set; }
}

public class ChangeStorageLimitRequest
{
	public required long NewSize { get; set; }
}

public class ChangeRegistrationRequest
{
	public required bool NewStatus { get; set; }
}

public class UpdateProfileRequest
{
	public required string Username { get; set; }
	public required string FirstName { get; set; }
	public required string LastName { get; set; }
	public required string EmailAddress { get; set; }
}

public class UpdateCredentialsRequest
{
	public required string Key { get; set; }
}

public class UpdatePasswordRequest
{
	public required string NewPassword { get; set; }
}

public class CreateFolderRequest
{
	public required string Name { get; set; }
	public required string ParentId { get; set; }
}
