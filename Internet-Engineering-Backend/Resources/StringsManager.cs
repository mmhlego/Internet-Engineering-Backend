using System.Reflection;
using System.Resources;

namespace Internet_Engineering_Backend.Resources;

public class StringsManager
{
	private readonly ResourceManager _errors = new ResourceManager("Internet_Engineering_Backend.Resources.Errors", Assembly.GetExecutingAssembly());

	public string GetErrorMessage(Errors key)
		=> GetStringFromResource(_errors, key.ToString(), "خطای سیستمی رخ داده است", "System Error");

	private string GetStringFromResource(ResourceManager resource, string key, string defaultPersian, string defaultEnglish)
	{
		try
		{
			var value = resource.GetString(key);

			if (value != null) return value;
		}
		catch (Exception)
		{ }

		if (Thread.CurrentThread.Name == "en-US") return defaultEnglish;
		return defaultPersian;
	}
}
