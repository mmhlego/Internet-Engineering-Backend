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
	private readonly StringsManager _strings;

	public AuthController(IMinioClient minioClient, DbContext dbContext, StringsManager strings)
	{
		_minioClient = minioClient;
		_dbContext = dbContext;
		_strings = strings;
	}

	[HttpPost]
	[Route("login")]
	public async Task<ActionResult> Login(LoginRequest request)
	{
		var user = _dbContext.Users.Find(f => f.Username == request.Username).FirstOrDefault();
		if (user == null) return BadRequest(_strings.GetErrorMessage(Errors.INVALID_LOGIN));

		var hashedPassword = (request.Password + user.Salt).GetSHA512();
		if (hashedPassword == user.Password)
			return BadRequest(_strings.GetErrorMessage(Errors.INVALID_LOGIN));

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
	public async Task<ActionResult> Register(RegisterRequest request)
	{
		var systemSettings = _dbContext.SystemSettings.Find(f => true).First();
		if (!systemSettings.CanRegister)
			return BadRequest(_strings.GetErrorMessage(Errors.REGISTRATION_CLOSED));

		if (_dbContext.Users.Find(f => f.Username == request.Username).Any())
			return BadRequest(_strings.GetErrorMessage(Errors.USERNAME_EXISTS));

		if (_dbContext.Users.Find(f => f.EmailAddress == request.EmailAddress).Any())
			return BadRequest(_strings.GetErrorMessage(Errors.EMAIL_EXISTS));

		if (request.Password.Length < 8
			|| request.Password.Contains(StringUtils.Symbols)
			|| request.Password.Contains(StringUtils.Digits)
			|| request.Password.Contains(StringUtils.LowerAlphabets)
			|| request.Password.Contains(StringUtils.UpperAlphabets))
			return BadRequest(_strings.GetErrorMessage(Errors.WEAK_PASSWORD));

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
		var args = new MakeBucketArgs().WithBucket(bucketName);
		await _minioClient.MakeBucketAsync(args);

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
	public async Task<ActionResult> Logout()
	{
		await HttpContext.SignOutAsync();

		return Ok();
	}
}
