using System.Diagnostics;
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
	[Route("test")]
	public async Task<ActionResult> Test(IFormFile file)
	{
		// var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

		// using (var ms = new MemoryStream())
		// {
		// file.CopyTo(ms);
		// var fileBytes = ms.ToArray();

		// var stream = file.OpenReadStream();
		// 		var name = Guid.NewGuid().ToString();
		// 		var args = new PutObjectArgs()
		// 			.WithBucket(userId)
		// 			.WithObject(name)
		// 			.WithObjectSize(file.Length)
		// 			.WithStreamData(file.OpenReadStream());
		// 		await _minioClient.PutObjectAsync(args);
		// 
		// 		return Ok($"localhost:9000/{userId}/{name}");

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

		if (request.Password.Length != 128)
			return BadRequest(Errors.INVALID_PASSWORD_LENGTH);

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

		// TODO
		// var jsonFormat = "{\"Version\":\"2012-10-17\",\"Statement\":[{\"Effect\":\"Allow\",\"Principal\":{\"AWS\":[\"*\"]},\"Action\":[\"s3:GetBucketLocation\",\"s3:ListBucket\",\"s3:ListBucketMultipartUploads\"],\"Resource\":[\"arn:aws:s3:::65b4ae718c321a19d0d4f9d4\"]},{\"Effect\":\"Allow\",\"Principal\":{\"AWS\":[\"*\"]},\"Action\":[\"s3:ListMultipartUploadParts\",\"s3:PutObject\",\"s3:AbortMultipartUpload\",\"s3:DeleteObject\",\"s3:GetObject\"],\"Resource\":[\"arn:aws:s3:::65b4ae718c321a19d0d4f9d4/*\"]}]}";
		// var updateArgs = new SetPolicyArgs().WithBucket(bucketName).WithPolicy(jsonFormat);
		// await _minioClient.SetPolicyAsync(updateArgs);

		var baseFolder = new Folder
		{
			Name = "",
			OwnerId = newUser.Id,
			ParentId = "",
		};

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
