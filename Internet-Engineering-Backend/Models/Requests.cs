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
