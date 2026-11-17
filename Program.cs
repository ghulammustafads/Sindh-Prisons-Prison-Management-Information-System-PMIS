using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using PrisonManagementSystem.Data;
using PrisonManagementSystem.Services;

var builder = WebApplication.CreateBuilder(args);

// Explicitly load configuration
builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile("appsettings.Database.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

// Connection String from configuration
var connectionString = builder.Configuration.GetConnectionString("PrisonDatabase");

// Normalize OLE DB format (e.g. from UDL files) to modern SqlClient format if needed
if (!string.IsNullOrEmpty(connectionString))
{
    connectionString = System.Text.RegularExpressions.Regex.Replace(
        connectionString,
        @"Provider\s*=\s*[^;]+;?",
        "",
        System.Text.RegularExpressions.RegexOptions.IgnoreCase).Trim();

    if (!connectionString.Contains("TrustServerCertificate", StringComparison.OrdinalIgnoreCase))
    {
        if (!connectionString.EndsWith(";")) connectionString += ";";
        connectionString += "TrustServerCertificate=True;";
    }

    if (!connectionString.Contains("Connect Timeout", StringComparison.OrdinalIgnoreCase))
    {
        if (!connectionString.EndsWith(";")) connectionString += ";";
        connectionString += "Connect Timeout=2;";
    }
}

// Probe SQL Server connection; fallback to local SQLite for continuous uptime and complete data operations
var sqlServerAvailable = false;
if (!string.IsNullOrEmpty(connectionString))
{
    try
    {
        using var testConn = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
        testConn.Open();
        sqlServerAvailable = true;
    }
    catch
    {
        sqlServerAvailable = false;
    }
}

// Register EF Core DbContext
builder.Services.AddDbContext<PrisonDbContext>(options =>
{
    if (sqlServerAvailable && !string.IsNullOrEmpty(connectionString))
    {
        options.UseSqlServer(connectionString, sqlOptions =>
        {
            sqlOptions.CommandTimeout(2);
            sqlOptions.EnableRetryOnFailure(maxRetryCount: 0);
        });
    }
    else
    {
        options.UseSqlite("Data Source=prison_system.db");
    }
});

// Register Domain & Infrastructure Services
builder.Services.AddScoped<ISchemaCompatibilityService, SchemaCompatibilityService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<ITimetableService, TimetableService>();
builder.Services.AddScoped<IGateRegisterService, GateRegisterService>();
builder.Services.AddScoped<ICourtService, CourtService>();
builder.Services.AddScoped<IPrisonerService, PrisonerService>();
builder.Services.AddScoped<IStaffService, StaffService>();

// Cookie Authentication configured for iframe compatibility
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.Cookie.Name = ".SindhPrisons.PMIS.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

// Authorization and MVC
builder.Services.AddAuthorization();
builder.Services.AddControllersWithViews();

builder.Services.AddAntiforgery(options =>
{
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Initialize database schema and initial data
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PrisonDbContext>();
    DbInitializer.Initialize(db);
}

app.UseDeveloperExceptionPage();

// Support HEAD requests for automated health checkers and reverse proxies
app.Use(async (context, next) =>
{
    if (HttpMethods.IsHead(context.Request.Method))
    {
        context.Request.Method = HttpMethods.Get;
        await next();
        return;
    }
    await next();
});

app.UseStaticFiles();

// Remove X-Frame-Options and allow iframe embedding
app.Use(async (context, next) =>
{
    context.Response.OnStarting(() =>
    {
        context.Response.Headers.Remove("X-Frame-Options");
        context.Response.Headers["Content-Security-Policy"] = "frame-ancestors *";
        return Task.CompletedTask;
    });

    // Check for query or header or cookie token for third-party cookie blocked browsers,
    // but ignore when explicit sign out was invoked
    if (!context.Request.Path.StartsWithSegments("/Account/Logout") &&
        !(context.Request.Path.StartsWithSegments("/Account/Login") && context.Request.Query.ContainsKey("loggedOut")))
    {
        string? token = context.Request.Query["t"].FirstOrDefault()
                     ?? context.Request.Query["auth"].FirstOrDefault()
                     ?? context.Request.Headers["X-Auth-Token"].FirstOrDefault()
                     ?? context.Request.Cookies["PMIS_AUTH_TOKEN"];

        if (!string.IsNullOrEmpty(token))
        {
            var principal = AuthTokenService.ValidateToken(token);
            if (principal != null)
            {
                context.User = principal;
            }
        }
    }

    await next();
});

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseSession();

// Map default controller route: default lands on Account/Login
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Index}/{id?}");

app.Run();
