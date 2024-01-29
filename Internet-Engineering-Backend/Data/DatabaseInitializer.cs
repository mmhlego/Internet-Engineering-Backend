using System.Security.Cryptography;
using Internet_Engineering_Backend.Models;
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
		var password = Environment.GetEnvironmentVariable("ADMIN_PASSWORD") ?? "password";
		var email = Environment.GetEnvironmentVariable("ADMIN_EMAIL") ?? "";

		var hashedPassword = password.GetSHA512();
		Aes aes = Aes.Create();
		var salt = aes.IV.ToHexString();

		dbContext.Users.InsertOne(new User
		{
			Username = username,
			Password = (hashedPassword + salt).GetSHA512(),
			EmailAddress = email,
			FirstName = "Admin",
			LastName = "Admin",
			Salt = salt,
			Role = UserRoles.Admin,
			EncryptionKey = "",
		});
	}
}
