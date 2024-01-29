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
	public ActionResult<StorageUsageResponse> GetStorageUsage()
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
	public ActionResult<object> CreateFolder([FromBody] CreateFolderRequest request)
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

		_dbContext.Activities.InsertOne(new Activity
		{
			IsFolder = true,
			ItemId = newFolder.Id,
			UserId = userId,
			Operation = Operations.Create,
		});

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

		return Ok(rootFolder.Id.ToString());
	}

	[HttpGet]
	[Route("folders/{id}")]
	public ActionResult<FolderContentResponse> GetFolderContent([FromRoute] string id, [FromQuery] int page, [FromQuery] int perPage, [FromQuery] string searchText = "", [FromQuery] bool onlyFolders = false, [FromQuery] string sort = "", [FromQuery] bool ascending = false)
	{
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

		var folder = _dbContext.Folders.Find(f => f.Id.ToString() == id && f.OwnerId == userId).FirstOrDefault();

		if (folder == null) return this.ErrorMessage(Errors.FOLDER_NOT_FOUND);

		if (sort.Equals("name"))
		{
			folder.SortOrder = SortOrders.Name;
			folder.SortAscending = ascending;

			_dbContext.Folders.ReplaceOne(f => f.Id == folder.Id, folder);
		}
		else if (sort.Equals("date"))
		{
			folder.SortOrder = SortOrders.CreationDate;
			folder.SortAscending = ascending;

			_dbContext.Folders.ReplaceOne(f => f.Id == folder.Id, folder);
		}

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
			IsFavorite = s.IsFavorite,
			IconColor = s.IconColor,
			Size = 0,
			IsShared = false,
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
				IsFavorite = s.IsFavorite,
				IconColor = s.IconColor,
				Size = s.ContentSize,
				IsShared = !s.IsEncrypted,
			});

			items.AddRange(files);
		}

		items.Sort(
			folder.SortOrder == SortOrders.Name
			? (folder.SortAscending ? Extensions.CompareNameAsc : Extensions.CompareNameDesc)
			: (folder.SortAscending ? Extensions.CompareDateAsc : Extensions.CompareDateDesc)
		);

		var data = Pagination<ItemResponse>.Paginate(page, perPage, items);
		var response = new FolderContentResponse
		{
			Data = data.Data,
			ItemsCount = data.ItemsCount,
			Page = data.Page,
			PerPage = data.PerPage,
			TotalPages = data.TotalPages,
			SortAscending = folder.SortAscending,
			SortOrder = folder.SortOrder == SortOrders.Name ? "name" : "date",
		};

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
					IsFavorite = file.IsFavorite,
					IconColor = file.IconColor,
					Size = file.ContentSize,
					IsShared = !file.IsEncrypted,
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
			IsFavorite = s.IsFavorite,
			IconColor = s.IconColor,
			Size = 0,
			IsShared = false,
		}).ToList();

		var files = _dbContext.Files.Find(f => f.OwnerId == userId && f.IsFavorite).ToList().Select(s => new ItemResponse
		{
			Id = s.Id,
			ItemType = s.ItemType.ToString(),
			Name = s.Name,
			CreationDate = s.CreationDate,
			IsFavorite = s.IsFavorite,
			IconColor = s.IconColor,
			Size = s.ContentSize,
			IsShared = !s.IsEncrypted,
		});

		items.AddRange(files);

		var response = Pagination<ItemResponse>.Paginate(page, perPage, items);

		return Ok(response);
	}

	#endregion


	#region Items

	[HttpGet]
	[Route("items/{id}/info")]
	public ActionResult<ItemInfoResponse> GetItemInfo([FromRoute] string id, [FromQuery] bool isFolder)
	{
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

		bool isShared = false;
		long size = 0;
		Item? item = null;
		if (isFolder)
			item = _dbContext.Folders.Find(f => f.Id.ToString() == id && f.OwnerId == userId).FirstOrDefault();
		else
		{
			var file = _dbContext.Files.Find(f => f.Id.ToString() == id && f.OwnerId == userId).FirstOrDefault();
			item = file;
			isShared = !file.IsEncrypted;
			size = file.ContentSize;
		}

		if (item == null)
			return this.ErrorMessage(isFolder ? Errors.FOLDER_NOT_FOUND : Errors.FILE_NOT_FOUND);

		var activities = _dbContext.Activities.Find(f => f.ItemId == id && f.IsFolder == isFolder)
			.ToList()
			.Select(s => new ItemActivity
			{
				Date = s.ModificationDate,
				Operation = s.Operation.ToString(),
			}).ToList();

		var info = new ItemInfoResponse
		{
			Id = item.Id,
			ItemType = item.ItemType.ToString(),
			Name = item.Name,
			CreationDate = item.CreationDate,
			IsFavorite = item.IsFavorite,
			IconColor = item.IconColor,
			Description = item.Description,
			Tags = item.Tags,
			IsShared = isShared,
			Size = size,
			Activities = activities,
		};

		return Ok(info);
	}

	[HttpPut]
	[Route("items/{id}/info")]
	public ActionResult<object> UpdateItemInfo([FromRoute] string id, [FromQuery] bool isFolder, [FromBody] UpdateInfoRequest request)
	{
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

		if (isFolder)
		{
			var folder = _dbContext.Folders.Find(f => f.Id.ToString() == id && f.OwnerId == userId).FirstOrDefault();
			if (folder == null)
				return this.ErrorMessage(Errors.FOLDER_NOT_FOUND);

			folder.Name = request.Name;
			folder.Tags = request.Tags;
			folder.Description = request.Description;

			_dbContext.Folders.ReplaceOne(f => f.Id == folder.Id, folder);

			_dbContext.Activities.InsertOne(new Activity
			{
				IsFolder = true,
				ItemId = folder.Id,
				UserId = userId,
				Operation = Operations.Modify,
			});
		}

		var file = _dbContext.Files.Find(f => f.Id.ToString() == id && f.OwnerId == userId).FirstOrDefault();

		file.Name = request.Name;
		file.Tags = request.Tags;
		file.Description = request.Description;

		_dbContext.Files.ReplaceOne(f => f.Id == file.Id, file);

		_dbContext.Activities.InsertOne(new Activity
		{
			IsFolder = false,
			ItemId = file.Id,
			UserId = userId,
			Operation = Operations.Modify,
		});

		return Ok();
	}

	[HttpGet]
	[Route("items/{id}/full-path")]
	public ActionResult<string> GetFullPath([FromRoute] string id, [FromQuery] bool isFolder)
	{
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

		Item? item = null;
		if (isFolder)
			item = _dbContext.Folders.Find(f => f.Id.ToString() == id && f.OwnerId == userId).FirstOrDefault();
		else
			item = _dbContext.Files.Find(f => f.Id.ToString() == id && f.OwnerId == userId).FirstOrDefault();

		if (item == null)
			return this.ErrorMessage(isFolder ? Errors.FOLDER_NOT_FOUND : Errors.FILE_NOT_FOUND);

		var fullPath = "";
		while (!string.IsNullOrEmpty(item.ParentId))
		{
			fullPath = "/" + item.Name + fullPath;
			item = _dbContext.Folders.Find(f => f.Id.ToString() == item.ParentId).First();
		}
		if (isFolder) fullPath += "/";

		return Ok(fullPath);
	}

	[HttpPost]
	[Route("items/{id}/favorite")]
	public ActionResult<object> SetFavorite([FromRoute] string id, [FromQuery] bool isFolder)
	{
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

		if (isFolder)
		{
			var folder = _dbContext.Folders.Find(f => f.Id.ToString() == id && f.OwnerId == userId).FirstOrDefault();
			if (folder == null)
				return this.ErrorMessage(Errors.FOLDER_NOT_FOUND);

			folder.IsFavorite = !folder.IsFavorite;
			_dbContext.Folders.ReplaceOne(f => f.Id == folder.Id, folder);
		}

		var file = _dbContext.Files.Find(f => f.Id.ToString() == id && f.OwnerId == userId).FirstOrDefault();
		file.IsFavorite = !file.IsFavorite;
		_dbContext.Files.ReplaceOne(f => f.Id == file.Id, file);

		return Ok();
	}

	[HttpPost]
	[Route("items/{id}/customize")]
	public ActionResult<object> CustomizeItem([FromRoute] string id, [FromQuery] bool isFolder, [FromBody] CustomizeRequest request)
	{
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

		if (isFolder)
		{
			var folder = _dbContext.Folders.Find(f => f.Id.ToString() == id && f.OwnerId == userId).FirstOrDefault();
			if (folder == null)
				return this.ErrorMessage(Errors.FOLDER_NOT_FOUND);

			folder.IconColor = request.Color;
			_dbContext.Folders.ReplaceOne(f => f.Id == folder.Id, folder);
		}

		var file = _dbContext.Files.Find(f => f.Id.ToString() == id && f.OwnerId == userId).FirstOrDefault();
		file.IconColor = request.Color;
		_dbContext.Files.ReplaceOne(f => f.Id == file.Id, file);

		return Ok();
	}

	#endregion


	#region Files

	[HttpPost]
	[Route("files")]
	public async Task<ActionResult<object>> UploadFile([FromForm] UploadFileRequest request)
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

		var type = ItemTypes.Others;
		if (new[] { "jpeg", "jpg", "png" }.Contains(request.Extension)) type = ItemTypes.Image;
		else if (new[] { "mp3", "m4a" }.Contains(request.Extension)) type = ItemTypes.Audio;
		else if (new[] { "mp4", "mkv", "mov" }.Contains(request.Extension)) type = ItemTypes.Video;
		else if (new[] { "txt", "docx", "pptx", "xlsx" }.Contains(request.Extension)) type = ItemTypes.Document;

		var newFile = new Models.File
		{
			Name = request.Name,
			OwnerId = userId,
			ParentId = folder.Id,
			ContentSize = request.File.Length,
			ItemType = type,
			ObjectName = objectId,
			IsEncrypted = request.IsEncrypted,
		};

		_dbContext.Files.InsertOne(newFile);

		_dbContext.Activities.InsertOne(new Activity
		{
			IsFolder = false,
			ItemId = newFile.Id,
			UserId = userId,
			Operation = Operations.Create,
		});

		return Ok();
	}

	[HttpDelete]
	[Route("files/{id}")]
	public async Task<ActionResult<object>> DeleteFile([FromRoute] string id, [FromBody] MoveFileRequest request)
	{
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

		var file = _dbContext.Files.Find(f => f.Id.ToString() == id && f.OwnerId == userId).FirstOrDefault();
		if (file == null)
			return this.ErrorMessage(Errors.FILE_NOT_FOUND);

		var args = new RemoveObjectArgs().WithBucket(userId).WithObject(file.ObjectName);
		await _minioClient.RemoveObjectAsync(args);

		_dbContext.Files.DeleteOne(f => f.Id == file.Id);
		_dbContext.ItemsSharing.DeleteMany(f => f.ItemId == file.Id);
		_dbContext.CustomLinks.DeleteMany(f => f.ItemId == file.Id);

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
	public ActionResult<object> MoveFile([FromRoute] string id, [FromBody] MoveFileRequest request)
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
	public ActionResult<object> ShareFileWithUser([FromRoute] string id, [FromBody] ShareFileRequest request)
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

		_dbContext.Activities.InsertOne(new Activity
		{
			IsFolder = false,
			ItemId = file.Id,
			UserId = userId,
			Operation = Operations.Create,
		});

		return Ok(newShare);
	}

	#endregion
}
