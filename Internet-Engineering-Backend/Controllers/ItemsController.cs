using System.Security.Claims;
using Internet_Engineering_Backend.Data;
using Internet_Engineering_Backend.Models;
using Internet_Engineering_Backend.Resources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Minio;
using Minio.DataModel.Args;
using MongoDB.Driver;

namespace Internet_Engineering_Backend.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public class ItemsController : ControllerBase
{
	private readonly IMinioClient _minioClient;
	private readonly DbContext _dbContext;

	public ItemsController(IMinioClient minioClient, DbContext dbContext)
	{
		_minioClient = minioClient;
		_dbContext = dbContext;
	}


	#region Used Space 

	[HttpGet]
	[Route("storage-usage")]
	public ActionResult<Pagination<ItemResponse>> GetStorageUsage()
	{
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

		var totalUsage = _dbContext.Files.Find(f => f.OwnerId == userId).ToList().Select(s => s.ContentSize).Sum();
		var settings = _dbContext.SystemSettings.Find(f => true).First();

		return Ok(new StorageUsageResponse
		{
			TotalUsage = totalUsage,
			TotalSpace = settings.StorageLimit,
		});
	}

	#endregion


	#region Folders

	[HttpPost]
	[Route("folders")]
	public ActionResult CreateFolder([FromBody] CreateFolderRequest request)
	{
		if (request.Name.Length < 1) return this.ErrorMessage(Errors.INVALID_NAME);

		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

		var folder = _dbContext.Folders.Find(f => f.Id.ToString() == request.ParentId && f.OwnerId == userId).FirstOrDefault();

		var newFolder = new Folder
		{
			OwnerId = userId,
			ParentId = folder.Id,
			Name = request.Name,
			Depth = folder.Depth + 1,
			ItemType = ItemTypes.Folder,
		};

		_dbContext.Folders.InsertOne(newFolder);

		return Ok();
	}

	[HttpGet]
	[Route("folders/root")]
	public async Task<ActionResult<string>> GetRootAddress()
	{
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
		var user = _dbContext.Users.Find(f => f.Id == userId).First();

		var existsArgs = new BucketExistsArgs().WithBucket(userId);
		if (!await _minioClient.BucketExistsAsync(existsArgs))
		{
			var bucketName = user.Id.ToString();
			var makeArgs = new MakeBucketArgs().WithBucket(bucketName);
			await _minioClient.MakeBucketAsync(makeArgs);

			var jsonFormat = "{\"Statement\":[{\"Action\":\"s3:*\",\"Effect\":\"Allow\",\"Principal\":\"*\",\"Resource\":\"arn:aws:s3:::" + bucketName + "/*\",\"Sid\":\"Set entirely public\"}],\"Version\":\"2012-10-17\"}";
			var updateArgs = new SetPolicyArgs().WithBucket(bucketName).WithPolicy(jsonFormat);
			await _minioClient.SetPolicyAsync(updateArgs);
		}

		var rootFolder = _dbContext.Folders.Find(f => f.OwnerId == userId && string.IsNullOrEmpty(f.ParentId)).FirstOrDefault();
		if (rootFolder == null)
		{
			rootFolder = new Folder
			{
				Depth = 0,
				ItemType = ItemTypes.Folder,
				Name = "",
				OwnerId = user.Id,
				ParentId = "",
			};

			_dbContext.Folders.InsertOne(rootFolder);
		}

		return Ok(rootFolder.Id);
	}

	[HttpGet]
	[Route("folders/{id}")]
	public ActionResult<Pagination<ItemResponse>> GetFolderContent([FromRoute] string id, [FromQuery] int page, [FromQuery] int perPage, [FromQuery] string searchText = "", [FromQuery] bool onlyFolders = false)
	{
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

		var folder = _dbContext.Folders.Find(f => f.Id.ToString() == id && f.OwnerId == userId).FirstOrDefault();

		if (folder == null) return this.ErrorMessage(Errors.FOLDER_NOT_FOUND);

		var items = _dbContext.Folders.Find(f =>
			f.ParentId == folder.Id &&
			(string.IsNullOrEmpty(searchText) ||
			f.Name.Contains(searchText) ||
			f.Description.Contains(searchText))
		).ToList().Select(s => new ItemResponse
		{
			Id = s.Id,
			ItemType = s.ItemType.ToString(),
			Name = s.Name,
			CreationDate = s.CreationDate,
		}).ToList();

		if (!onlyFolders)
		{
			var files = _dbContext.Files.Find(f =>
				f.ParentId == folder.Id &&
				(string.IsNullOrEmpty(searchText) ||
				f.Name.Contains(searchText) ||
				f.Description.Contains(searchText))
			).ToList().Select(s => new ItemResponse
			{
				Id = s.Id,
				ItemType = s.ItemType.ToString(),
				Name = s.Name,
				CreationDate = s.CreationDate,
			});

			items.AddRange(files);
		}

		var response = Pagination<ItemResponse>.Paginate(page, perPage, items);

		return Ok(response);
	}

	#endregion


	#region Favorites and Shared

	[HttpGet]
	[Route("shared")]
	public ActionResult<Pagination<ItemResponse>> GetSharedFiled([FromQuery] int page, [FromQuery] int perPage)
	{
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

		var sharedItems = _dbContext.ItemsSharing.Find(f => f.UserId == userId).ToList();

		var pagination = Pagination<ItemSharing>.Paginate(page, perPage, sharedItems);
		var items = new List<ItemResponse>();

		pagination.Data.ForEach(item =>
		{
			var file = _dbContext.Files.Find(f => f.Id.ToString() == item.Id && !f.IsEncrypted).FirstOrDefault();

			if (file != null)
			{

				items.Add(new ItemResponse
				{
					Id = file.Id,
					ItemType = file.ItemType.ToString(),
					Name = file.Name,
					CreationDate = item.ShareDate,
				});
			}
		});

		var response = new Pagination<ItemResponse>
		{
			Page = pagination.Page,
			PerPage = pagination.PerPage,
			TotalPages = pagination.TotalPages,
			ItemsCount = pagination.ItemsCount,
			Data = items,
		};

		return Ok(response);
	}

	[HttpGet]
	[Route("shared/{id}")]
	public async Task<ActionResult<string>> GetShareUrl([FromRoute] string id)
	{
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

		var file = _dbContext.Files.Find(f => f.Id.ToString() == id && f.OwnerId == userId).FirstOrDefault();
		if (file == null)
			return this.ErrorMessage(Errors.FILE_NOT_FOUND);

		if (file.IsEncrypted)
			return this.ErrorMessage(Errors.FILE_NOT_SHARED);

		var share = _dbContext.ItemsSharing.Find(f => f.UserId == userId && f.ItemId == file.Id).FirstOrDefault();
		if (share == null)
			return this.ErrorMessage(Errors.FILE_NOT_SHARED);

		var args = new PresignedGetObjectArgs().WithBucket(file.OwnerId).WithObject(file.ObjectName).WithExpiry(60 * 60);
		var link = await _minioClient.PresignedGetObjectAsync(args);

		return Ok(link);
	}

	[HttpGet]
	[Route("favorites")]
	public ActionResult<Pagination<ItemResponse>> GetFavorites([FromQuery] int page, [FromQuery] int perPage)
	{
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

		var items = _dbContext.Folders.Find(f => f.OwnerId == userId && f.IsFavorite).ToList().Select(s => new ItemResponse
		{
			Id = s.Id,
			ItemType = s.ItemType.ToString(),
			Name = s.Name,
			CreationDate = s.CreationDate,
		}).ToList();

		var files = _dbContext.Files.Find(f => f.OwnerId == userId && f.IsFavorite).ToList().Select(s => new ItemResponse
		{
			Id = s.Id,
			ItemType = s.ItemType.ToString(),
			Name = s.Name,
			CreationDate = s.CreationDate,
		});

		items.AddRange(files);

		var response = Pagination<ItemResponse>.Paginate(page, perPage, items);

		return Ok(response);
	}

	#endregion


	#region Items

	// TODO
	[HttpGet]
	[Route("items/{id}/info")]
	public ActionResult GetItemInfo([FromRoute] string id, [FromRoute] bool isFolder) => throw new NotImplementedException();

	// TODO
	[HttpPut]
	[Route("items/{id}/info")]
	public ActionResult UpdateItemInfo([FromRoute] string id, [FromRoute] bool isFolder) => throw new NotImplementedException();

	// TODO
	[HttpGet]
	[Route("items/{id}/full-path")]
	public ActionResult GetFullPath([FromRoute] string id, [FromRoute] bool isFolder) => throw new NotImplementedException();

	// TODO
	[HttpPost]
	[Route("items/{id}/favorite")]
	public ActionResult SetFavorite([FromRoute] string id, [FromRoute] bool isFolder) => throw new NotImplementedException();

	// TODO
	[HttpPost]
	[Route("items/{id}/customize")]
	public ActionResult CustomizeItem([FromRoute] string id, [FromRoute] bool isFolder) => throw new NotImplementedException();

	#endregion


	#region Files

	[HttpPost]
	[Route("files")]
	public async Task<ActionResult> UploadFile([FromForm] UploadFileRequest request)
	{
		if (request.Name.Length < 1) return this.ErrorMessage(Errors.INVALID_NAME);

		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

		var totalUsage = _dbContext.Files.Find(f => f.OwnerId == userId).ToList().Select(s => s.ContentSize).Sum();
		var settings = _dbContext.SystemSettings.Find(f => true).First();

		if (totalUsage + request.File.Length > settings.StorageLimit)
			return this.ErrorMessage(Errors.STORAGE_LIMIT_EXCEEDED);

		var folder = _dbContext.Folders.Find(f => f.Id.ToString() == request.ParentId && f.OwnerId == userId).FirstOrDefault();

		if (folder == null)
			return this.ErrorMessage(Errors.FOLDER_NOT_FOUND);

		var objectId = Guid.NewGuid().ToString().Replace("-", "");

		var putObjectArgs = new PutObjectArgs()
								.WithBucket(userId)
								.WithStreamData(request.File.OpenReadStream())
								.WithObject(objectId)
								.WithObjectSize(request.File.Length);

		var res = await _minioClient.PutObjectAsync(putObjectArgs);

		var newFile = new Models.File
		{
			Name = request.Name,
			OwnerId = userId,
			ParentId = folder.Id,
			ContentSize = request.File.Length,
			ItemType = ItemTypes.Others,
			ObjectName = objectId,
			IsEncrypted = request.IsEncrypted,
		};

		_dbContext.Files.InsertOne(newFile);

		return Ok();
	}

	[HttpGet]
	[Route("files/{id}/download")]
	public async Task<ActionResult<string>> GetDownloadLink([FromRoute] string id)
	{
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

		var file = _dbContext.Files.Find(f => f.Id.ToString() == id && f.OwnerId == userId).FirstOrDefault();
		if (file == null)
			return this.ErrorMessage(Errors.FILE_NOT_FOUND);

		var args = new PresignedGetObjectArgs().WithBucket(userId).WithObject(file.ObjectName).WithExpiry(60 * 60);
		var link = await _minioClient.PresignedGetObjectAsync(args);

		return Ok(link);
	}

	[HttpPost]
	[Route("files/{id}/move")]
	public ActionResult MoveFile([FromRoute] string id, [FromBody] MoveFileRequest request)
	{
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

		var file = _dbContext.Files.Find(f => f.Id.ToString() == id && f.OwnerId == userId).FirstOrDefault();
		if (file == null)
			return this.ErrorMessage(Errors.FILE_NOT_FOUND);

		var folder = _dbContext.Folders.Find(f => f.Id.ToString() == request.TargetFolderId && f.OwnerId == userId).FirstOrDefault();
		if (folder == null)
			return this.ErrorMessage(Errors.FOLDER_NOT_FOUND);

		file.ParentId = folder.Id;
		_dbContext.Files.ReplaceOne(f => f.Id == file.Id, file);

		return Ok();
	}

	[HttpPost]
	[Route("files/{id}/share/user")]
	[Authorize]
	public ActionResult ShareFileWithUser([FromRoute] string id, [FromBody] ShareFileRequest request)
	{
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

		var user = _dbContext.Users.Find(f => f.Username == request.TargetUser || f.EmailAddress == request.TargetUser).FirstOrDefault();
		if (user == null)
			return this.ErrorMessage(Errors.USER_NOT_FOUND);

		var file = _dbContext.Files.Find(f => f.Id.ToString() == id && f.OwnerId == userId).FirstOrDefault();
		if (file == null)
			return this.ErrorMessage(Errors.FILE_NOT_FOUND);

		if (file.IsEncrypted)
			return this.ErrorMessage(Errors.FILE_NOT_SHARED);

		var newShare = new ItemSharing
		{
			ItemId = file.Id,
			UserId = user.Id,
		};
		_dbContext.ItemsSharing.InsertOne(newShare);

		return Ok();
	}

	[HttpPost]
	[Route("files/{id}/share/custom")]
	[Authorize]
	public ActionResult<CustomLink> GenerateCustomShare([FromRoute] string id, [FromBody] CustomShareRequest request)
	{
		if (request.Usage == 0)
			return this.ErrorMessage(Errors.INVALID_USAGE_VALUE);

		if (request.ExpiryDate.CompareTo(DateTime.UtcNow) <= 0)
			return this.ErrorMessage(Errors.INVALID_EXPIRY_DATE);

		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

		var file = _dbContext.Files.Find(f => f.Id.ToString() == id && f.OwnerId == userId).FirstOrDefault();
		if (file == null)
			return this.ErrorMessage(Errors.FILE_NOT_FOUND);

		if (file.IsEncrypted)
			return this.ErrorMessage(Errors.FILE_NOT_SHARED);

		var newShare = new CustomLink
		{
			ItemId = file.Id,
			ExpiryDate = request.ExpiryDate,
			Usage = request.Usage,
		};
		_dbContext.CustomLinks.InsertOne(newShare);

		return Ok(newShare);
	}

	#endregion
}
