using System.Net;
using Newtonsoft.Json;

namespace Internet_Engineering_Backend.Middlewares;

public class ExceptionHandlingMiddleware
{
	public RequestDelegate requestDelegate;
	public ExceptionHandlingMiddleware(RequestDelegate requestDelegate)
	{
		this.requestDelegate = requestDelegate;
	}

	public async Task Invoke(HttpContext context)
	{
		try
		{
			await requestDelegate(context);
		}
		catch (Exception ex)
		{
			await HandleException(context, ex);
		}
	}
	private Task HandleException(HttpContext context, Exception ex)
	{
		var errorMessage = JsonConvert.SerializeObject(null);
		context.Response.ContentType = "application/json";
		context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
		return context.Response.WriteAsync(errorMessage);
	}
}
