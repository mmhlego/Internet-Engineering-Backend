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
[Route("api/[controller]")]
public class ShareController : ControllerBase
{
	private readonly IMinioClient _minioClient;
	private readonly DbContext _dbContext;

	public ShareController(IMinioClient minioClient, DbContext dbContext)
	{
		_minioClient = minioClient;
		_dbContext = dbContext;
	}

	[HttpGet]
	[Route("{id}")]
	public async Task<ActionResult<string>> GetCustomShareUrl([FromRoute] string id)
	{
		var share = _dbContext.CustomLinks.Find(f => f.Id.ToString() == id).FirstOrDefault();
		if (share == null)
			return this.ErrorMessage(Errors.INVALID_SHARE_URL);

		if (share.ExpiryDate.CompareTo(DateTime.UtcNow) < 0 || share.Usage == 0)
			return this.ErrorMessage(Errors.SHARE_EXPIRED);

		var file = _dbContext.Files.Find(f => f.Id.ToString() == share.ItemId).FirstOrDefault();
		if (file == null)
			return this.ErrorMessage(Errors.FILE_NOT_FOUND);

		if (file.IsEncrypted)
			return this.ErrorMessage(Errors.FILE_NOT_SHARED);

		var args = new PresignedGetObjectArgs().WithBucket(file.OwnerId).WithObject(file.ObjectName).WithExpiry(60 * 60);
		var link = await _minioClient.PresignedGetObjectAsync(args);

		return Redirect(link);
	}
}
