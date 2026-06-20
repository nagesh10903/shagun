using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Shagun.DBRepo;
using Shagun.Services.Interfaces.IUserService;
using Shagun.Services.UserService;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.Extensions.FileProviders;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// -------Add services to the container. ---------

// Add DbContext with MySQL provider
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

//Dependency Injection 
builder.Services.AddScoped<IUserService, UserService>();



builder.Services.AddControllers();

// JWT Authentication (basic)
var jwtKey = builder.Configuration["Jwt:Key"] ?? "replace_this_in_config";
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

builder.Services.AddAuthentication(options => {
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options => {
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = !string.IsNullOrEmpty(jwtIssuer),
        ValidateAudience = !string.IsNullOrEmpty(jwtAudience),
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ValidateLifetime = true,
        NameClaimType = JwtRegisteredClaimNames.Sub,
        RoleClaimType = "role"
    };
});

// Ensure JWT handler reads our claims correctly
JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

// Current user helper
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<Shagun.Services.CurrentUserService>();
builder.Services.AddHttpClient("razorpay");
builder.Services.AddSingleton<Shagun.Services.RazorpayService>();
builder.Services.AddScoped<Shagun.Services.AuthService>();
builder.Services.AddScoped<Shagun.Services.GiftService>();
builder.Services.AddScoped<Shagun.Services.PaymentService>();
builder.Services.AddScoped<Shagun.Services.NotificationService>();

// CORS: allow frontend dev and preview origins
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        var allowed = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
        if (allowed.Length > 0)
        {
            policy.WithOrigins(allowed)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        }
        else
        {
            // No origins configured: be conservative and do not allow cross-origin by default.
            policy.SetIsOriginAllowed(_ => false);
        }
    });
});


var app = builder.Build();

// ------- Configure the HTTP request pipeline.---------

// Serve static files from frontend build (frontend/dist) or frontend folder.
var frontendDist = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "..", "frontend", "dist"));
var frontendSrc = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "..", "frontend"));

if (Directory.Exists(frontendDist))
{
    var provider = new PhysicalFileProvider(frontendDist);
    app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = provider });
    app.UseStaticFiles(new StaticFileOptions { FileProvider = provider });
}
else if (Directory.Exists(frontendSrc))
{
    var provider = new PhysicalFileProvider(frontendSrc);
    app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = provider });
    app.UseStaticFiles(new StaticFileOptions { FileProvider = provider });
}

// Apply CORS globally before routing so preflight OPTIONS are handled
app.UseCors("AllowFrontend");
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// SPA fallback to index.html if present
app.MapFallback(async context => {
    var filePath = Directory.Exists(frontendDist) ? Path.Combine(frontendDist, "index.html") : Path.Combine(frontendSrc, "index.html");
    if (File.Exists(filePath))
    {
        context.Response.ContentType = "text/html; charset=utf-8";
        await context.Response.SendFileAsync(filePath);
        return;
    }
    context.Response.StatusCode = 404;
});

app.Run();
