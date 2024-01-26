using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Internet_Engineering_Backend.Utils;

public class StringUtils
{
	private static readonly Random _random = new Random();
	public static readonly string UpperAlphabets = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
	public static readonly string LowerAlphabets = "abcdefghijklmnopqrstuvwxyz";
	public static readonly string Digits = "0123456789";
	public static readonly string Symbols = "-_=+(){{}}[]*&^%$#@!";

	public static string GenerateSalt() => Guid.NewGuid().ToString();

	public static string RandomString(int length, bool includeNumber = false)
	{
		var letters = UpperAlphabets + (includeNumber ? Digits : "");

		return new string(Enumerable.Repeat(letters, length).Select(s => s[_random.Next(s.Length)]).ToArray());
	}

	public static byte[] EncryptString(string plainText, byte[] Key, byte[] IV)
	{
		byte[] encrypted;
		using (Aes aes = Aes.Create())
		{
			ICryptoTransform encryptor = aes.CreateEncryptor(Key, IV);
			using MemoryStream ms = new MemoryStream();
			using CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
			using (StreamWriter sw = new StreamWriter(cs))
				sw.Write(plainText);
			encrypted = ms.ToArray();
		}
		return encrypted;
	}

	public static string DecryptString(byte[] cipherText, byte[] Key, byte[] IV)
	{
		string plaintext = "";
		using Aes aes = Aes.Create();
		ICryptoTransform decryptor = aes.CreateDecryptor(Key, IV);

		using MemoryStream ms = new MemoryStream(cipherText);
		using CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
		using StreamReader reader = new StreamReader(cs);

		plaintext = reader.ReadToEnd();

		return plaintext;
	}
}
