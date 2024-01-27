using Internet_Engineering_Backend.Data;
using Internet_Engineering_Backend.Resources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Internet_Engineering_Backend;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class SystemController : ControllerBase
{
	private readonly DbContext _dbContext;
	private readonly StringsManager _strings;

	public SystemController(DbContext dbContext, StringsManager strings)
	{
		_dbContext = dbContext;
		_strings = strings;
	}

	[HttpGet]
	[Route("users")]
	public ActionResult GetUsers() => throw new NotImplementedException();

	[HttpPost]
	[Route("users")]
	public ActionResult AddUser() => throw new NotImplementedException();

	[HttpPut]
	[Route("users/{id}")]
	public ActionResult UpdateUser(string id) => throw new NotImplementedException();

	[HttpGet]
	[Route("storage-usage")]
	public ActionResult GetStorageUsage() => throw new NotImplementedException();

	[HttpGet]
	[Route("user-storage")]
	public ActionResult GetUserStorage() => throw new NotImplementedException();

	[HttpPut]
	[Route("user-storage")]
	public ActionResult UpdateUserStorage() => throw new NotImplementedException();

	[HttpGet]
	[Route("registration")]
	public ActionResult GetRegistration() => throw new NotImplementedException();

	[HttpPut]
	[Route("registration")]
	public ActionResult UpdateRegistration() => throw new NotImplementedException();
}

