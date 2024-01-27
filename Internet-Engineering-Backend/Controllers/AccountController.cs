using Internet_Engineering_Backend.Data;
using Internet_Engineering_Backend.Resources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Internet_Engineering_Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AccountController : ControllerBase
{
	private readonly DbContext _dbContext;
	private readonly StringsManager _strings;

	public AccountController(DbContext dbContext, StringsManager strings)
	{
		_dbContext = dbContext;
		_strings = strings;
	}

	[HttpGet]
	[Route("profile")]
	public ActionResult GetProfile() => throw new NotImplementedException();

	[HttpPut]
	[Route("profile")]
	public ActionResult UpdateProfile() => throw new NotImplementedException();

	[HttpGet]
	[Route("secret-key")]
	public ActionResult GetEncryptionKey() => throw new NotImplementedException();

	[HttpPut]
	[Route("secret-key")]
	public ActionResult UpdateEncryptionKey() => throw new NotImplementedException();

	[HttpPut]
	[Route("change-password")]
	public ActionResult UpdatePassword() => throw new NotImplementedException();
}
