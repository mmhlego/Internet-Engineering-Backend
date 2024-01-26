using Internet_Engineering_Backend.Data;
using Internet_Engineering_Backend.Resources;
using Microsoft.AspNetCore.Mvc;
using Minio;

namespace Internet_Engineering_Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
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

	// [Route("sample")]

}
