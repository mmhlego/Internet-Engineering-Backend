using Internet_Engineering_Backend.Data;
using Internet_Engineering_Backend.Middlewares;
using Microsoft.AspNetCore.Authentication.Cookies;
using Minio;

var builder = WebApplication.CreateBuilder(args);


builder.Services.Configure<DatabaseSettings>(builder.Configuration.GetSection("DatabaseSettings"));

builder.Services.AddSingleton(
	new MinioClient()
		.WithEndpoint(builder.Configuration.GetSection("MinioSettings:Address").Get<string>())
		.WithCredentials(
			Environment.GetEnvironmentVariable("MINIO_ROOT_USER"),
			Environment.GetEnvironmentVariable("MINIO_ROOT_PASSWORD")
		)
		.Build()
);

builder.Services.AddTransient<DbContext>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddAuthentication(options =>
	{
		options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
		options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
		options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
	})
	.AddCookie(options =>
	{
		options.LoginPath = "/api/Auth/login";
		options.AccessDeniedPath = "/api/Auth/logout";
		options.LogoutPath = "/logout";
		options.ExpireTimeSpan = TimeSpan.FromDays(1);
	});
builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
	options.AddPolicy("CorsPolicy",
		builder => builder.SetIsOriginAllowedToAllowWildcardSubdomains()
						   .SetIsOriginAllowed(origin => new Uri(origin).Host == "localhost" || new Uri(origin).Host == "127.0.0.1")
						   .AllowCredentials()
						   .AllowAnyMethod()
						   .AllowAnyHeader()
	);
});

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}
else
{
	app.UseHsts();
}

app.UseCors("CorsPolicy");
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Initialize Database
using (var scope = app.Services.CreateScope())
{
	var context = scope.ServiceProvider.GetRequiredService<DbContext>();

	DatabaseInitializer.Start(context);
}

app.Run();
