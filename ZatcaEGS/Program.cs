using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Localization;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

var configuration = new ConfigurationBuilder()
    .AddCommandLine(args)
    .Build();

// Check command-line arguments --p xxxx
var portArg = configuration["p"];

if (portArg != null && int.TryParse(portArg, out int parsedPort))
{
    if (parsedPort != 80 || parsedPort != 443 || parsedPort != 0)
    {
        //Console.WriteLine($"Using port: {parsedPort}");

        builder.WebHost.ConfigureKestrel(options =>
        {
            //options.Listen(System.Net.IPAddress.Any, parsedPort, listenOptions =>
            //{
            //    listenOptions.UseHttps();
            //});

            options.Listen(System.Net.IPAddress.Any, parsedPort); // HTTP
        });
    }
}

// Configure host based on OS platform
if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
{
    // Use Windows Service for Windows
    builder.Host.UseWindowsService();
}
else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
{
    builder.Host.UseSystemd();
}

// Configure JSON options
builder.Services.Configure<JsonOptions>(options =>
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault | JsonIgnoreCondition.WhenWritingNull);

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins",
        builder => builder.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader());
});

// Add controllers with views
builder.Services.AddControllersWithViews();


var app = builder.Build();

// Configure middleware
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Uncomment this for HTTPS redirection if needed
// app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseRouting();
app.UseCors("AllowAllOrigins");

// Use custom middleware
//app.UseMiddleware<DisclaimerMiddleware>();

// Use request localization
var defaultCulture = new CultureInfo("en-US");
var localizationOptions = new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(defaultCulture),
    SupportedCultures = new List<CultureInfo> { defaultCulture },
    SupportedUICultures = new List<CultureInfo> { defaultCulture }
};
app.UseRequestLocalization(localizationOptions);

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();


