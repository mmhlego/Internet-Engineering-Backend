using Internet_Engineering_Backend.Data;
using Internet_Engineering_Backend.Middlewares;
using Internet_Engineering_Backend.Resources;
using Microsoft.AspNetCore.Authentication.Cookies;
using Minio;

var builder = WebApplication.CreateBuilder(args);


builder.Services.Configure<DatabaseSettings>(builder.Configuration.GetSection("DatabaseSettings"));

builder.Services.AddSingleton(
	new MinioClient()
		.WithEndpoint(builder.Configuration.GetSection("MinioSettings:Address").Get<string>())
		.WithCredentials(
			Environment.GetEnvironmentVariable("MINIO_ACCESS_KEY"),
			Environment.GetEnvironmentVariable("MINIO_SECRET_KEY")
		)
		.Build()
);

builder.Services.AddSingleton<StringsManager>();
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

string[] allowedHosts = (builder.Configuration.GetSection("AllowedHosts").Value ?? "").Split(";");
builder.Services.AddCors(options =>
{
	options.AddPolicy("CorsPolicy",
		builder => builder.SetIsOriginAllowedToAllowWildcardSubdomains()
						   .WithOrigins(allowedHosts)
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
