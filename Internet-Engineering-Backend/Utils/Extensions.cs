using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Internet_Engineering_Backend.Models;
using Internet_Engineering_Backend.Resources;
using Microsoft.AspNetCore.Mvc;

namespace Internet_Engineering_Backend;

public static class Extensions
{
	#region String Extensions

	public static byte[] NormalToByteArray(this string str) => Encoding.ASCII.GetBytes(str);

	public static byte[] HexToByteArray(this string hexString)
	{
		byte[] data = new byte[hexString.Length / 2];
		for (int index = 0; index < data.Length; index++)
		{
			string byteValue = hexString.Substring(index * 2, 2);
			data[index] = byte.Parse(byteValue, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
		}

		return data;
	}

	public static string GetSHA256(this string str) =>
		BitConverter.ToString(SHA256.HashData(Encoding.UTF8.GetBytes(str))).Replace("-", "");

	public static string GetSHA512(this string str) =>
		BitConverter.ToString(SHA512.HashData(Encoding.UTF8.GetBytes(str))).Replace("-", "");

	#endregion


	#region Byte Array Extensions

	public static string ToHexString(this byte[] array) =>
		BitConverter.ToString(array).Replace("-", "");

	#endregion


	#region Byte Array Extensions

	public static BadRequestObjectResult ErrorMessage(this ControllerBase controller, Errors error) =>
		controller.BadRequest(StringsManager.GetErrorMessage(error));

	#endregion


	#region Item Response Comparison

	public static int CompareNameAsc(ItemResponse item1, ItemResponse item2) => item1.Name.CompareTo(item2.Name);

	public static int CompareNameDesc(ItemResponse item1, ItemResponse item2) => item2.Name.CompareTo(item1.Name);

	public static int CompareDateAsc(ItemResponse item1, ItemResponse item2) => item1.CreationDate.CompareTo(item2.CreationDate);

	public static int CompareDateDesc(ItemResponse item1, ItemResponse item2) => item2.CreationDate.CompareTo(item1.CreationDate);

	#endregion
}
