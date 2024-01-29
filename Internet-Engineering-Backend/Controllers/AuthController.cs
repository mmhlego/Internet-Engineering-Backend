using System.Security.Claims;
using Internet_Engineering_Backend.Data;
using Internet_Engineering_Backend.Models;
using Internet_Engineering_Backend.Resources;
using Internet_Engineering_Backend.Utils;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Minio;
using Minio.DataModel.Args;
using MongoDB.Driver;

namespace Internet_Engineering_Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
	private readonly IMinioClient _minioClient;
	private readonly DbContext _dbContext;

	public AuthController(IMinioClient minioClient, DbContext dbContext)
	{
		_minioClient = minioClient;
		_dbContext = dbContext;
	}

	[HttpPost]
	[Route("login")]
	public async Task<ActionResult<object>> Login(LoginRequest request)
	{
		var user = _dbContext.Users.Find(f => f.Username == request.Username).FirstOrDefault();
		if (user == null) return this.ErrorMessage(Errors.INVALID_LOGIN);

		var hashedPassword = (request.Password + user.Salt).GetSHA512();
		if (!hashedPassword.Equals(user.Password, StringComparison.CurrentCultureIgnoreCase))
			return this.ErrorMessage(Errors.INVALID_LOGIN);

		if (user.IsRestricted)
			return this.ErrorMessage(Errors.USER_RESTRICTED);

		var claims = new List<Claim>
		{
			new Claim(ClaimTypes.NameIdentifier, user.Id),
			new Claim(ClaimTypes.Role, user.Role.ToString()),
		};
		var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
		await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

		return Ok();
	}

	[HttpPost]
	[Route("register")]
	public async Task<ActionResult<object>> Register(RegisterRequest request)
	{
		var systemSettings = _dbContext.SystemSettings.Find(f => true).First();
		if (!systemSettings.CanRegister)
			return this.ErrorMessage(Errors.REGISTRATION_CLOSED);

		if (_dbContext.Users.Find(f => f.Username == request.Username).Any())
			return this.ErrorMessage(Errors.USERNAME_EXISTS);

		if (_dbContext.Users.Find(f => f.EmailAddress == request.EmailAddress).Any())
			return this.ErrorMessage(Errors.EMAIL_EXISTS);

		if (request.Password.Length != 128)
			return this.ErrorMessage(Errors.INVALID_PASSWORD_LENGTH);

		var salt = StringUtils.GenerateSalt().GetSHA256()[..32];
		var hashedPassword = (request.Password + salt).GetSHA512();

		var newUser = new User
		{
			Username = request.Username,
			FirstName = request.FirstName,
			LastName = request.LastName,
			EmailAddress = request.EmailAddress,
			Salt = salt,
			Password = hashedPassword,
			EncryptionKey = "",
		};

		_dbContext.Users.InsertOne(newUser);

		var bucketName = newUser.Id.ToString();
		var makeArgs = new MakeBucketArgs().WithBucket(bucketName);
		await _minioClient.MakeBucketAsync(makeArgs);

		var jsonFormat = "{\"Statement\":[{\"Action\":\"s3:*\",\"Effect\":\"Allow\",\"Principal\":\"*\",\"Resource\":\"arn:aws:s3:::" + bucketName + "/*\",\"Sid\":\"Set entirely public\"}],\"Version\":\"2012-10-17\"}";
		var updateArgs = new SetPolicyArgs().WithBucket(bucketName).WithPolicy(jsonFormat);
		await _minioClient.SetPolicyAsync(updateArgs);

		_dbContext.Folders.InsertOne(new Folder
		{
			ItemType = ItemTypes.Folder,
			Name = "",
			Depth = 0,
			OwnerId = newUser.Id,
			ParentId = "",
		});

		var claims = new List<Claim>
		{
			new Claim(ClaimTypes.NameIdentifier, newUser.Id),
			new Claim(ClaimTypes.Role, newUser.Role.ToString()),
		};
		var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
		await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

		return Ok();
	}

	[HttpGet]
	[Route("logout")]
	public async Task<ActionResult<object>> Logout()
	{
		await HttpContext.SignOutAsync();

		return Ok();
	}
}
