using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Server.Data;
using Server.Models;
using Server.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "VideoHosting API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme.",
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
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Host=localhost;Database=videohosting;Username=postgres;Password=postgres";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IVideoService, VideoService>();
builder.Services.AddScoped<IVideoProcessingService, VideoProcessingService>();
builder.Services.AddScoped<IInteractionService, InteractionService>();
builder.Services.AddScoped<IModerationService, ModerationService>();

// JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? "YourSuperSecretKeyForDevelopment2024MinLength32Chars";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "VideoHostingServer",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "VideoHostingClient",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Configuration
builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

var app = builder.Build();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        context.Database.EnsureCreated();
        
        // Create admin user if not exists
        if (!context.Users.Any(u => u.Role == "admin"))
        {
            var adminUser = new User
            {
                Id = Guid.NewGuid(),
                Login = "admin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin"),
                Role = "admin",
                CreatedAt = DateTime.UtcNow
            };
            context.Users.Add(adminUser);
            
            // Create moderator user
            var moderatorUser = new User
            {
                Id = Guid.NewGuid(),
                Login = "moderator",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("moderator"),
                Role = "moderator",
                CreatedAt = DateTime.UtcNow
            };
            context.Users.Add(moderatorUser);
            
            context.SaveChanges();
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Database initialization error: {ex.Message}");
    }
}

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Serve static files (uploaded videos)
var uploadsPath = builder.Configuration["UploadsPath"] ?? Path.Combine(builder.Environment.ContentRootPath, "uploads");
if (!Directory.Exists(uploadsPath))
{
    Directory.CreateDirectory(uploadsPath);
}

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads"
});

app.Run();
