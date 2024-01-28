using System.Security.Claims;
using Internet_Engineering_Backend.Data;
using Internet_Engineering_Backend.Models;
using Internet_Engineering_Backend.Resources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace Internet_Engineering_Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AccountController : ControllerBase
{
	private readonly DbContext _dbContext;

	public AccountController(DbContext dbContext)
	{
		_dbContext = dbContext;
	}

	[HttpGet]
	[Route("profile")]
	public ActionResult<ProfileResponse> GetProfile()
	{
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
		var user = _dbContext.Users.Find(f => f.Id.ToString() == userId).First();

		return Ok(new ProfileResponse
		{
			Username = user.Username,
			FirstName = user.FirstName,
			LastName = user.LastName,
			EmailAddress = user.EmailAddress,
			Role = user.Role.ToString(),
		});
	}

	[HttpPut]
	[Route("profile")]
	public ActionResult UpdateProfile([FromBody] UpdateProfileRequest request)
	{
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
		var user = _dbContext.Users.Find(f => f.Id.ToString() == userId).First();

		if (_dbContext.Users.Find(f => f.Id != userId && f.Username == request.Username).Any())
			return this.ErrorMessage(Errors.USERNAME_EXISTS);

		if (_dbContext.Users.Find(f => f.Id != userId && f.EmailAddress == request.EmailAddress).Any())
			return this.ErrorMessage(Errors.EMAIL_EXISTS);

		user.Username = request.Username;
		user.FirstName = request.FirstName;
		user.LastName = request.LastName;
		user.EmailAddress = request.EmailAddress;

		_dbContext.Users.ReplaceOne(f => f.Id == user.Id, user);

		return Ok();
	}

	[HttpGet]
	[Route("credentials")]
	public ActionResult<CredentialsResponse> GetEncryptionKey()
	{
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
		var user = _dbContext.Users.Find(f => f.Id.ToString() == userId).First();

		return Ok(new CredentialsResponse
		{
			IV = user.Salt,
			Key = user.EncryptionKey,
		});
	}

	[HttpPut]
	[Route("credentials")]
	public ActionResult UpdateEncryptionKey([FromBody] UpdateCredentialsRequest request)
	{
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
		var user = _dbContext.Users.Find(f => f.Id.ToString() == userId).First();

		if (request.Key.Length != 32)
			return this.ErrorMessage(Errors.INVALID_KEY_LENGTH);

		user.EncryptionKey = request.Key;
		_dbContext.Users.ReplaceOne(f => f.Id == user.Id, user);

		return Ok();
	}

	[HttpPut]
	[Route("change-password")]
	public ActionResult UpdatePassword([FromBody] UpdatePasswordRequest request)
	{
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
		var user = _dbContext.Users.Find(f => f.Id.ToString() == userId).First();

		if (request.NewPassword.Length != 128)
			return this.ErrorMessage(Errors.INVALID_PASSWORD_LENGTH);

		user.Password = (request.NewPassword + user.Salt).GetSHA512();
		_dbContext.Users.ReplaceOne(f => f.Id == user.Id, user);

		return Ok();
	}
}
