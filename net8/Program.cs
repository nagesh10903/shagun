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
using System.Diagnostics;
using System.Runtime.InteropServices;
using Shagun.Swagger;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// -------Add services to the container. ---------

// Add DbContext with MySQL provider
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

//Dependency Injection 
builder.Services.AddScoped<IUserService, UserService>();



builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "Shagun API", Version = "v1" });
    c.OperationFilter<FileUploadOperationFilter>();
    // Add JWT bearer auth to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: 'Authorization: Bearer {token}'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            new string[] { }
        }
    });
});

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

// Configure Razorpay service implementation via configuration
var useMockRazorpay = builder.Configuration.GetValue<bool?>("Razorpay:UseMock") ?? true;
if (useMockRazorpay)
{
    builder.Services.AddSingleton<Shagun.Services.Interfaces.IRazorpayService.IRazorpayService, Shagun.Services.MockRazorpayService>();
}
else
{
    builder.Services.AddSingleton<Shagun.Services.Interfaces.IRazorpayService.IRazorpayService, Shagun.Services.RazorpayService>();
}
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
                  .AllowCredentials()
                  .SetIsOriginAllowed(origin => true);
        }
        else
        {
            // No origins configured: be conservative and do not allow cross-origin by default.
            policy.SetIsOriginAllowed(_ => true);
        }
    });
});


var app = builder.Build();

// ------- Configure the HTTP request pipeline.---------

// Serve static files from frontend build (frontend/dist) or frontend folder.
var frontendDist = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "..", "wwwroot", "assets"));
var frontendSrc = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "..", "wwwroot"));

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

// Swagger will be enabled in development after routing is configured below.

// Serve uploaded files from /uploads (uploads/gifts etc.)
var uploadsPath = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "uploads"));
Directory.CreateDirectory(uploadsPath);
{
    var uploadProvider = new PhysicalFileProvider(uploadsPath);
    app.UseStaticFiles(new StaticFileOptions { FileProvider = uploadProvider, RequestPath = "/uploads" });
}

// Attempt to ensure uploads folder is writable by the process on Unix systems
try
{
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
    {
        var psi = new ProcessStartInfo
        {
            FileName = "chmod",
            Arguments = $"-R 0775 \"{uploadsPath}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        using var proc = Process.Start(psi);
        if (proc != null)
        {
            proc.WaitForExit(3000);
            var outText = proc.StandardOutput.ReadToEnd();
            var errText = proc.StandardError.ReadToEnd();
            if (proc.ExitCode == 0)
            {
                app.Logger.LogInformation("Set permissions on uploads folder: {UploadsPath}", uploadsPath);
            }
            else
            {
                app.Logger.LogWarning("chmod exited {Code} for uploads folder. stderr: {Err}", proc.ExitCode, errText);
            }
        }
    }
}
catch (System.Exception ex)
{
    app.Logger.LogWarning(ex, "Failed to set uploads folder permissions automatically");
}

// Apply CORS globally before routing so preflight OPTIONS are handled
app.UseCors("AllowFrontend");
app.UseRouting();

// Enable Swagger in development and point UI to the generated JSON
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Shagun API v1"));
}
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
