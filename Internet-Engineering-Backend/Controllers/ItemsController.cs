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
		if (await _minioClient.BucketExistsAsync(existsArgs))
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
			Data = items,
		};

		return Ok(response);
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

	// TODO
	[HttpPost]
	[Route("files")]
	public ActionResult UploadFile() => throw new NotImplementedException();

	// TODO
	[HttpPost]
	[Route("files/{id}/move")]
	public ActionResult MoveFile([FromRoute] string id) => throw new NotImplementedException();

	// TODO
	[HttpPost]
	[Route("files/{id}/share/user")]
	public ActionResult ShareFileWithUser([FromRoute] string id) => throw new NotImplementedException();

	// TODO
	[HttpPost]
	[Route("files/{id}/share/custom")]
	public ActionResult GenerateCustomShare([FromRoute] string id) => throw new NotImplementedException();

	#endregion
}
