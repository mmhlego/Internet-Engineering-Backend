using Internet_Engineering_Backend.Data;
using Internet_Engineering_Backend.Models;
using Internet_Engineering_Backend.Resources;
using Internet_Engineering_Backend.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Minio;
using Minio.DataModel.Args;
using MongoDB.Driver;

namespace Internet_Engineering_Backend;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class SystemController : ControllerBase
{
	private readonly IMinioClient _minioClient;
	private readonly DbContext _dbContext;

	public SystemController(IMinioClient minioClient, DbContext dbContext)
	{
		_minioClient = minioClient;
		_dbContext = dbContext;
	}

	[HttpGet]
	[Route("users")]
	public ActionResult<Pagination<UserResponse>> GetUsers([FromQuery] int page, [FromQuery] int perPage)
	{
		var users = _dbContext.Users.Find(f => f.Role == UserRoles.Basic).ToList().Select(s => new UserResponse
		{
			Username = s.Username,
			FirstName = s.FirstName,
			LastName = s.LastName,
			EmailAddress = s.Password,
			Restricted = s.Restricted,
		});

		var res = Pagination<UserResponse>.Paginate(page, perPage, users.ToList());

		return Ok(res);
	}

	[HttpPost]
	[Route("users")]
	public async Task<ActionResult> AddUser([FromBody] RegisterRequest request)
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

		// TODO: Create key for user

		var bucketName = newUser.Id.ToString();
		var makeArgs = new MakeBucketArgs().WithBucket(bucketName);
		await _minioClient.MakeBucketAsync(makeArgs);

		var jsonFormat = "{\"Statement\":[{\"Action\":\"s3:*\",\"Effect\":\"Allow\",\"Principal\":\"*\",\"Resource\":\"arn:aws:s3:::" + bucketName + "/*\",\"Sid\":\"Set entirely public\"}],\"Version\":\"2012-10-17\"}";
		var updateArgs = new SetPolicyArgs().WithBucket(bucketName).WithPolicy(jsonFormat);
		await _minioClient.SetPolicyAsync(updateArgs);

		var baseFolder = new Folder
		{
			Depth = 0,
			Name = "",
			ItemType = ItemTypes.Folder,
			OwnerId = newUser.Id,
			ParentId = "",
		};

		return Ok();
	}

	[HttpPut]
	[Route("users/{id}")]
	public ActionResult UpdateUser([FromRoute] string id, [FromBody] ChangeRegistrationRequest request)
	{
		var user = _dbContext.Users.Find(f => f.Role == UserRoles.Basic && f.Id.ToString() == id).FirstOrDefault();

		if (user == null) return this.ErrorMessage(Errors.USER_NOT_FOUND);

		user.Restricted = request.NewStatus;
		_dbContext.Users.ReplaceOne(f => f.Id == user.Id, user);

		return Ok();
	}

	[HttpGet]
	[Route("storage-usage")]
	public ActionResult<StorageUsageResponse> GetStorageUsage()
	{
		var usages = _dbContext.Files.Find(f => true).ToList().Select(s => s.ContentSize);
		var totalUsage = usages.Sum();

		var res = CommandExecuter.RunCommand("df", "--output=avail /home").Replace(" ", "").Split()[1];
		_ = long.TryParse(res, out long availableSpace);

		return Ok(new StorageUsageResponse
		{
			TotalUsage = totalUsage,
			TotalSpace = availableSpace + totalUsage,
		});
	}

	[HttpGet]
	[Route("user-storage")]
	public ActionResult<long> GetUserStorage()
	{
		var settings = _dbContext.SystemSettings.Find(f => true).First();

		return Ok(settings.StorageLimit);
	}

	[HttpPut]
	[Route("user-storage")]
	public ActionResult<long> UpdateUserStorage([FromBody] ChangeStorageLimitRequest request)
	{
		var settings = _dbContext.SystemSettings.Find(f => true).First();

		settings.StorageLimit = request.NewSize;
		_dbContext.SystemSettings.ReplaceOne(f => f.Id == settings.Id, settings);

		return Ok(settings.StorageLimit);
	}

	[HttpGet]
	[Route("registration")]
	public ActionResult<bool> GetRegistration()
	{
		var settings = _dbContext.SystemSettings.Find(f => true).First();

		return Ok(settings.CanRegister);
	}

	[HttpPut]
	[Route("registration")]
	public ActionResult<bool> UpdateRegistration([FromBody] ChangeRegistrationRequest request)
	{
		var settings = _dbContext.SystemSettings.Find(f => true).First();

		settings.CanRegister = request.NewStatus;
		_dbContext.SystemSettings.ReplaceOne(f => f.Id == settings.Id, settings);

		return Ok(settings.CanRegister);
	}
}

