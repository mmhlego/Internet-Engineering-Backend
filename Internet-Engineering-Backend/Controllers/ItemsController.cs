using System.Security.Claims;
using Internet_Engineering_Backend.Data;
using Internet_Engineering_Backend.Resources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Minio;
using MongoDB.Driver;

namespace Internet_Engineering_Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ItemsController : ControllerBase
{
	private readonly IMinioClient _minioClient;
	private readonly DbContext _dbContext;
	private readonly StringsManager _strings;

	public ItemsController(IMinioClient minioClient, DbContext dbContext, StringsManager strings)
	{
		_minioClient = minioClient;
		_dbContext = dbContext;
		_strings = strings;
	}

	#region Folders

	[HttpPost]
	[Route("/folders")]
	public ActionResult CreateFolder() => throw new NotImplementedException();

	[HttpGet]
	[Route("/folders/{id}")]
	public ActionResult GetFolderContent([FromRoute] string? id, [FromQuery] string searchText = "", [FromQuery] bool onlyFolders = false)
	{
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

		var folder = id == null
		? _dbContext.Folders.Find(f => f.Id.ToString() == id && f.OwnerId == userId).FirstOrDefault()
		: _dbContext.Folders.Find(f => f.Name.Length == 0 && f.OwnerId == userId).First();

		if (folder == null) return NotFound();

		throw new NotImplementedException();
	}

	[HttpPut]
	[Route("/folders/{id}/sorts")]
	public ActionResult UpdateSortOrder([FromRoute] string? id) => throw new NotImplementedException();

	[HttpPut]
	[Route("/folders/{id}/info")]
	public ActionResult UpdateFolder([FromRoute] string? id) => throw new NotImplementedException();

	#endregion


	#region Favorites and Shared

	[HttpGet]
	[Route("/shared")]
	public ActionResult GetSharedFiled([FromRoute] string? id) => throw new NotImplementedException();

	[HttpGet]
	[Route("/favorites")]
	public ActionResult GetFavorites([FromRoute] string? id) => throw new NotImplementedException();

	[HttpGet]
	[Route("/items/{id}/full-path")]
	public ActionResult GetFullPath([FromRoute] string? id) => throw new NotImplementedException();

	[HttpPost]
	[Route("/items/{id}/favorite")]
	public ActionResult SetFavorite([FromRoute] string? id) => throw new NotImplementedException();

	[HttpPost]
	[Route("/items/{id}/customize")]
	public ActionResult CustomizeItem([FromRoute] string? id) => throw new NotImplementedException();

	#endregion


	#region Files

	[HttpPost]
	[Route("/files")]
	public ActionResult UploadFile() => throw new NotImplementedException();

	[HttpGet]
	[Route("/files/{id}/info")]
	public ActionResult GetFileInfo([FromRoute] string? id) => throw new NotImplementedException();

	[HttpPut]
	[Route("/files/{id}/info")]
	public ActionResult UpdateFileInfo([FromRoute] string? id) => throw new NotImplementedException();

	[HttpPost]
	[Route("/files/{id}/move")]
	public ActionResult MoveFile([FromRoute] string? id) => throw new NotImplementedException();

	[HttpPost]
	[Route("/files/{id}/share/user")]
	public ActionResult ShareFileWithUser([FromRoute] string? id) => throw new NotImplementedException();

	[HttpPost]
	[Route("/files/{id}/share/custom")]
	public ActionResult GenerateCustomShare([FromRoute] string? id) => throw new NotImplementedException();

	#endregion
}
