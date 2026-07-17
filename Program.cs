using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using ChatAgenda.Data;
using ChatAgenda.Services;
using ChatAgenda.Hubs;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.WebHost.UseUrls("http://0.0.0.0:5002");

    // Add Database Context
    var dataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ChatAgenda");
    Directory.CreateDirectory(dataDir);
    var dbPath = Path.Combine(dataDir, "chatagenda.db");

    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlite($"Data Source={dbPath}"));

    // Configure Cookie Authentication
    builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
        .AddCookie(options =>
        {
            options.Cookie.Name = "ChatAgenda.Auth";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.LoginPath = "/login.html";
            options.Events.OnRedirectToLogin = context =>
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            };
        });

    // Add Controllers & SignalR
    builder.Services.AddControllers();
    builder.Services.AddSignalR();

    // Register Google Sync Service (Dual registration for injection in controllers & as background service)
    builder.Services.AddSingleton<GoogleCalendarSyncService>();
    builder.Services.AddHostedService(sp => sp.GetRequiredService<GoogleCalendarSyncService>());

    var app = builder.Build();

    // Auto-create/migrate database on startup
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.EnsureCreated();

        // Cleanup seeded users if they exist
        var toRemove = db.Users.Where(u => u.Username == "ana" || u.Username == "juan" || u.Username == "pedro").ToList();
        if (toRemove.Any())
        {
            db.Users.RemoveRange(toRemove);
            db.SaveChanges();
        }
    }

    // HTTP pipeline configuration

    // Enable serving static files from wwwroot
    app.UseDefaultFiles();
    app.UseStaticFiles();

    var uploadsFolder = Path.Combine(dataDir, "uploads");
    Directory.CreateDirectory(uploadsFolder);
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(uploadsFolder),
        RequestPath = "/uploads"
    });

    app.UseRouting();

    app.UseAuthentication();
    app.UseAuthorization();

    // Map endpoints and hubs
    app.MapControllers();
    app.MapHub<ChatHub>("/chatHub");

    Console.WriteLine("==============================================");
    Console.WriteLine("  ChatAgenda Servidor Iniciado");
    Console.WriteLine("  Escuchando en: http://0.0.0.0:5002");
    Console.WriteLine("  Base de datos: " + dbPath);
    Console.WriteLine("  NO CIERRES ESTA VENTANA");
    Console.WriteLine("==============================================");

    app.Run();
}
catch (Exception ex)
{
    Console.WriteLine("==============================================");
    Console.WriteLine("  ERROR AL INICIAR ChatAgenda:");
    Console.WriteLine("==============================================");
    Console.WriteLine(ex.ToString());
    Console.WriteLine();
    Console.WriteLine("Presiona ENTER para cerrar...");
    Console.ReadLine();
}
