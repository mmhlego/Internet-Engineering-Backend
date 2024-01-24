using System.Security.Cryptography;
using Internet_Engineering_Backend.Models;
using Internet_Engineering_Backend.Utils;
using MongoDB.Driver;

namespace Internet_Engineering_Backend.Data;

public class DatabaseInitializer
{
	public static void Start(DbContext dbContext)
	{
		AddOwnerAccount(dbContext);
	}

	private static void AddOwnerAccount(DbContext dbContext)
	{
		if (dbContext.Users.Find(f => f.Role == UserRoles.Admin).FirstOrDefault() != null) return;

		var username = Environment.GetEnvironmentVariable("ADMIN_USERNAME") ?? "Admin";
		var password = Environment.GetEnvironmentVariable("ADMIN_PASSWORD") ?? "password";
		var email = Environment.GetEnvironmentVariable("ADMIN_EMAIL") ?? "";


		Aes aes = Aes.Create();
		var iv = StringUtils.ByteArrayToHexString(aes.IV);
		var key = StringUtils.ByteArrayToHexString(aes.Key);

		aes.Key = StringUtils.HexStringToByteArray(StringUtils.HashString(password, ""));

		// dbContext.Users.InsertOne(new User
		// {
		// 	Username = username,
		// 	Password = StringUtils.HashString(password, iv),
		// 	EmailAddress = email,
		// 	FirstName = "Admin",
		// 	LastName = "Admin",
		// 	Salt = iv,
		// 	Role = UserRoles.Admin,
		// 	EncryptionKey = StringUtils.EncryptString(),
		// });
	}
}
