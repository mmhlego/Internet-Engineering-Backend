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
		AddSettings(dbContext);
	}

	private static void AddSettings(DbContext dbContext)
	{
		if (dbContext.SystemSettings.Find(f => true).Any()) return;

		dbContext.SystemSettings.InsertOne(new SystemSettings
		{
			StorageLimit = 1_000_000_000,
			CanRegister = false,
		});
	}

	private static void AddOwnerAccount(DbContext dbContext)
	{
		if (dbContext.Users.Find(f => f.Role == UserRoles.Admin).FirstOrDefault() != null) return;

		var username = Environment.GetEnvironmentVariable("ADMIN_USERNAME") ?? "Admin";
		var password = (Environment.GetEnvironmentVariable("ADMIN_PASSWORD") ?? "password").GetSHA512();
		var email = Environment.GetEnvironmentVariable("ADMIN_EMAIL") ?? "";

		var hashedPassword = password;
		Aes aes = Aes.Create();
		var salt = aes.IV.ToHexString();
		var key = password.GetSHA256();
		var x = "";
		x.GetSHA256();

		dbContext.Users.InsertOne(new User
		{
			Username = username,
			Password = (password + salt).GetSHA512(),
			EmailAddress = email,
			FirstName = "Admin",
			LastName = "Admin",
			Salt = salt,
			Role = UserRoles.Admin,
			EncryptionKey = StringUtils.EncryptString(key, password.GetSHA256().HexToByteArray(), aes.IV).ToHexString(),
		});
	}
}
