using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Reflection;
using System.Text.Json.Serialization;
using ZatcaEGS.Helpers;
using ZatcaEGS.Models;

//dotnet build --self-contained -p:GenerateRuntimeConfigurationFiles=false -p:GenerateDependencyFile=false

var builder = WebApplication.CreateSlimBuilder(args);

var portArg = args.FirstOrDefault(arg => arg.StartsWith("-p="));
var dbPathArg = args.FirstOrDefault(arg => arg.StartsWith("-d="));

int port = 4454;
string dbPath = Path.Combine(AppContext.BaseDirectory, "Data", "ZatcaEGS.db");

if (portArg != null && int.TryParse(portArg.Split('=')[1], out int parsedPort))
{
    port = parsedPort;
}

if (dbPathArg != null)
{
    dbPath = dbPathArg.Split('=')[1];
}

var dbDirectory = Path.GetDirectoryName(dbPath);
if (!string.IsNullOrEmpty(dbDirectory) && !Directory.Exists(dbDirectory))
{
    Directory.CreateDirectory(dbDirectory);
}

dbPath = $"Data Source={dbPath}";

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(port);
    options.ListenAnyIP((port + 1), listenOptions =>
    {
        listenOptions.UseHttps();
    });
});


//var certPath = Path.Combine(AppContext.BaseDirectory, "Cert", "certificate.pfx"); // @"C:\tmp\Cert\certificate.pfx";

//builder.WebHost.ConfigureKestrel(options =>
//{
//    options.ListenAnyIP(port); 

//    var httpsPort = port + 1; 

//    options.ListenAnyIP(httpsPort, listenOptions =>
//    {
//        try
//        {
//            var cert = new X509Certificate2(certPath, @"Mabaega", X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable);
//            listenOptions.UseHttps(cert);
//        }
//        catch (Exception ex)
//        {
//            Console.WriteLine($"Error loading certificate: {ex.Message}");
//            throw; // Rethrow the exception to halt application startup
//        }
//    });
//});


builder.Host.UseWindowsService();
builder.Services.AddWindowsService();

builder.Services.Configure<JsonOptions>(options =>
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault | JsonIgnoreCondition.WhenWritingNull);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins",
        builder => builder.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader());
});


builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(dbPath));
builder.Services.AddControllersWithViews();

var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0.0";
builder.Services.AddSingleton(new AppVersionService(version));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

using (var scope = app.Services.CreateScope())
{
    var _dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    _dbContext.Database.EnsureCreated();
}

var defaultCulture = new CultureInfo("en-US");
var localizationOptions = new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(defaultCulture),
    SupportedCultures = new List<CultureInfo> { defaultCulture },
    SupportedUICultures = new List<CultureInfo> { defaultCulture }
};

app.UseRequestLocalization(localizationOptions);

//app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
