using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SmartGridAPI.Data;
using SmartGridAPI.Middleware;
using SmartGridAPI.Services;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Disable reloadOnChange to prevent inotify crashes on Linux/Render free tier
builder.Configuration.Sources.Clear();
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);
builder.Configuration.AddEnvironmentVariables();

// ─────────────────────────────────────────────
// 1. Database — MySQL via Pomelo
// ─────────────────────────────────────────────
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// ─────────────────────────────────────────────
// 2. JWT Authentication
// ─────────────────────────────────────────────
var jwtSecret = builder.Configuration["JwtSettings:SecretKey"]
    ?? throw new InvalidOperationException("JWT SecretKey not configured.");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
        ValidAudience = builder.Configuration["JwtSettings:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ClockSkew = TimeSpan.Zero
    };
    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = ctx =>
        {
            if (ctx.Exception.GetType() == typeof(SecurityTokenExpiredException))
                ctx.Response.Headers.Append("Token-Expired", "true");
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

// ─────────────────────────────────────────────
// 3. CORS
// ─────────────────────────────────────────────
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:5173", "http://localhost:3000" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("SmartGridCORS", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// ─────────────────────────────────────────────
// 4. Application Services
// ─────────────────────────────────────────────
builder.Services.AddScoped<IAIService, AIService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddHttpClient();

// ─────────────────────────────────────────────
// 5. Controllers
// ─────────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(opts =>
    {
        opts.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        opts.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        opts.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });

// ─────────────────────────────────────────────
// 6. Swagger / OpenAPI
// ─────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Smart Grid Monitoring API",
        Version = "v1",
        Description = "API for Smart Electricity Grid Monitoring System with AI Integration",
        Contact = new OpenApiContact { Name = "Smart Grid Team", Email = "admin@smartgrid.com" }
    });

    // JWT in Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization. Enter: Bearer {your_token}",
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
            Array.Empty<string>()
        }
    });
});

// ─────────────────────────────────────────────
// Build App
// ─────────────────────────────────────────────
var app = builder.Build();

// ─────────────────────────────────────────────
// Middleware Pipeline
// ─────────────────────────────────────────────
app.UseMiddleware<ExceptionMiddleware>();

app.UseStaticFiles();

// Swagger enabled in ALL environments for easy testing
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Smart Grid API v1");
    c.RoutePrefix = "swagger";
    c.DocumentTitle = "Smart Grid Monitoring API";
});

app.UseCors("SmartGridCORS");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// ─────────────────────────────────────────────
// Auto-migrate database on startup
// ─────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        await context.Database.MigrateAsync();
        Console.WriteLine("✅ Database migration applied successfully.");

        // Fix database seeded password hashes
        var admin = await context.Users.FirstOrDefaultAsync(u => u.Username == "admin");
        if (admin != null)
        {
            admin.PasswordHash = "$2a$11$t3KQzl4lh40owPIXHkxXCOJpfYSMIvH/YF6CygLub4220I5GMGXnm";
            admin.Email = "admin@smartgrid.com";
        }

        var operatorUser = await context.Users.FirstOrDefaultAsync(u => u.Username == "operator" || u.Username == "electricity_officer");
        if (operatorUser != null)
        {
            operatorUser.Username = "electricity_officer";
            operatorUser.PasswordHash = "$2a$11$t3KQzl4lh40owPIXHkxXCOJpfYSMIvH/YF6CygLub4220I5GMGXnm";
            operatorUser.Email = "officer@smartgrid.com";
            operatorUser.Role = "Electricity Officer";
        }

        var teamUser = await context.Users.FirstOrDefaultAsync(u => u.Username == "maintenance_team");
        if (teamUser != null)
        {
            teamUser.PasswordHash = "$2a$11$t3KQzl4lh40owPIXHkxXCOJpfYSMIvH/YF6CygLub4220I5GMGXnm";
        }

        // Apply Kanyakumari Area Updates dynamically
        var nodes = await context.GridNodes.ToListAsync();
        foreach (var node in nodes)
        {
            if (node.Id == 1) { node.Location = "Nagercoil Substation"; node.Latitude = 8.1833; node.Longitude = 77.4119; }
            if (node.Id == 2) { node.Location = "Marthandam Industrial Zone"; node.Latitude = 8.3114; node.Longitude = 77.2065; }
            if (node.Id == 3) { node.Location = "Kanyakumari Residential"; node.Latitude = 8.0883; node.Longitude = 77.5385; }
            if (node.Id == 4) { node.Location = "Colachel Commercial District"; node.Latitude = 8.1794; node.Longitude = 77.2619; }
            if (node.Id == 5) { node.Location = "Padmanabhapuram Substation"; node.Latitude = 8.2483; node.Longitude = 77.3308; }
        }

        // Patch old Faults descriptions to match new nodes
        var faults = await context.Faults.ToListAsync();
        foreach (var fault in faults)
        {
            fault.Description = fault.Description
                .Replace("Downtown Substation", "Nagercoil Substation")
                .Replace("Industrial Zone", "Marthandam Industrial Zone")
                .Replace("Residential Area North", "Kanyakumari Residential")
                .Replace("Commercial District", "Colachel Commercial District")
                .Replace("Eastern Substation", "Padmanabhapuram Substation");
        }

        // Patch old Outages affected areas
        var outages = await context.Outages.ToListAsync();
        foreach (var outage in outages)
        {
            outage.AffectedArea = outage.AffectedArea
                .Replace("Downtown Substation", "Nagercoil Substation")
                .Replace("Industrial Zone", "Marthandam Industrial Zone")
                .Replace("Residential Area North", "Kanyakumari Residential")
                .Replace("Commercial District", "Colachel Commercial District")
                .Replace("Eastern Substation", "Padmanabhapuram Substation");
        }

        await context.SaveChangesAsync();
        Console.WriteLine("✅ Seeded user password hashes and Kanyakumari areas verified/updated in database.");
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating the database.");
        Console.WriteLine($"❌ Database migration failed: {ex.Message}");
        Console.WriteLine("   Make sure MySQL is running and credentials in appsettings.json are correct.");
    }
}

Console.WriteLine("🔌 Smart Grid Monitoring API started!");
Console.WriteLine("📊 Swagger UI: http://localhost:5000/swagger");
Console.WriteLine("📡 API Base URL: http://localhost:5000");

app.Run();