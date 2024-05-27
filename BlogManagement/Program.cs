using BlogManagement.DBContext;
using BlogManagement.Hubs;
using BlogManagement.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Net.Mail;
using System.Net;
using System.Text;
using BlogManagement.Services.Interface;
using BlogManagement.Services.Implementation;
using BlogManagement.Seed;
using BlogManagement.Infrastructure;
using BlogManagement.Schedular;
using Hangfire;
using Hangfire.PostgreSql;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDBContext>(options =>
    options.UseNpgsql(connectionString));

var mongoSettings = builder.Configuration.GetSection("MongoDB");
builder.Services.AddSingleton(new MongoDbContext(
    mongoSettings["ConnectionString"],
    mongoSettings["DatabaseName"]
));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = true;
    options.Tokens.AuthenticatorTokenProvider = TokenOptions.DefaultAuthenticatorProvider;
    options.Lockout.AllowedForNewUsers = true;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 10;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDBContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromDays(1);
    options.SlidingExpiration = true;
});

builder.Services.AddSession(options =>
{
    options.Cookie.Name = ".MyApp.Session";
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Adjust the session timeout as needed
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdminRole", policy => policy.RequireRole("Admin"));
    options.AddPolicy("RequireUserRole", policy => policy.RequireRole("User"));
});
builder.Services.AddSignalR();
builder.Services.AddRazorPages();
builder.Services.AddLogging(loggingBuilder => loggingBuilder.AddSerilog());

builder.Services.AddScoped<RoleSeeder>();
var serviceProvider = builder.Services.BuildServiceProvider();
var roleSeeder = serviceProvider.GetRequiredService<RoleSeeder>();
var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
await roleSeeder.SeedRolesAsync(roleManager);

builder.Services.Configure<SMTPConfig>(builder.Configuration.GetSection("SMTPConfig"));
builder.Services.AddSingleton(resolver => resolver.GetRequiredService<IOptions<SMTPConfig>>().Value);
builder.Services.AddSingleton<SmtpClient>(sp =>
{
    var smtpSettings = sp.GetRequiredService<SMTPConfig>();
    var smtpClient = new SmtpClient
    {
        Host = smtpSettings.SmtpHost,
        Port = smtpSettings.SmtpPort,
        EnableSsl = smtpSettings.SmtpSsl,
        DeliveryMethod = SmtpDeliveryMethod.Network,
        UseDefaultCredentials = false,
        Credentials = new NetworkCredential(smtpSettings.SmtpUsername, smtpSettings.SmtpPassword)
    };
    return smtpClient;
});
builder.Services.AddSingleton<IEmailService, EmailService>();
builder.Services.AddHangfire(config => config.UsePostgreSqlStorage(connectionString));
builder.Services.AddSingleton<PurgeArchivedBlogPostsJob>();

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .WriteTo.MongoDB(builder.Configuration.GetConnectionString("MongoDb"))
    .CreateLogger();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();
app.UseHangfireServer();

app.UseHangfireDashboard();
RecurringJob.AddOrUpdate<PurgeArchivedBlogPostsJob>("PurgeArchivedBlogPosts",
    job => job.Execute(null),
    "*/1 * * * *");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();
app.MapHub<BlogHub>("/blogHub");
app.Run();
