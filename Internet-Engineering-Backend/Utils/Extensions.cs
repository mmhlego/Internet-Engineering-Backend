using System.Globalization;
using System.Security.Cryptography;
using System.Text;

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
}
