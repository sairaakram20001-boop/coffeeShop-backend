using Microsoft.EntityFrameworkCore;
using CoffeeShop.Data;
using CoffeeShop.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json.Serialization; // Required for ReferenceHandler
using Microsoft.Extensions.Configuration;

var builder = WebApplication.CreateBuilder(args);
const string FrontendCorsPolicy = "FrontendCorsPolicy";

LoadDotEnv(Path.Combine(builder.Environment.ContentRootPath, ".env"), builder.Configuration);

// Avoid Windows EventLog permission issues in local dev environments.
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// 1. Controllers with JSON Cycle Fix
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Yeh line "Object Cycle Detected" error ko khatam kar degi
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

// 2. DB Context
var dbConnection = builder.Configuration["DB_CONNECTION"];
if (string.IsNullOrWhiteSpace(dbConnection))
{
    dbConnection = builder.Configuration.GetConnectionString("DefaultConnection");
}

if (string.IsNullOrWhiteSpace(dbConnection))
{
    throw new Exception("Database connection string is missing.");
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(dbConnection)
);

// 3. JWT Authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");
var keyString = jwtSettings["Key"] ?? throw new Exception("JWT Key is missing in appsettings.json");
var key = Encoding.UTF8.GetBytes(keyString);

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
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});

// 4. Dependency Injection (DI)
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<CartService>();
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<CategoryService>();

// 5. CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicy, policy =>
    {
        var configuredOrigins = builder.Configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>()
            ?? Array.Empty<string>();

        var frontendUrl = builder.Configuration["FRONTEND_URL"];
        if (!string.IsNullOrWhiteSpace(frontendUrl))
        {
            configuredOrigins = configuredOrigins
                .Append(frontendUrl.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        if (configuredOrigins.Length > 0)
        {
            policy.WithOrigins(configuredOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod();
            return;
        }

        policy.SetIsOriginAllowed(origin =>
            Uri.TryCreate(origin, UriKind.Absolute, out var uri)
            && (uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
                || uri.Host.Equals("127.0.0.1")))
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

// 6. Middleware Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseStaticFiles();
app.UseCors(FrontendCorsPolicy);

app.UseAuthentication(); // Always before Authorization
app.UseAuthorization();

app.MapControllers();

app.Run();

static void LoadDotEnv(string envPath, ConfigurationManager configuration)
{
    if (!File.Exists(envPath))
    {
        return;
    }

    var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

    foreach (var rawLine in File.ReadAllLines(envPath))
    {
        var line = rawLine.Trim();
        if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
        {
            continue;
        }

        var separatorIndex = line.IndexOf('=');
        if (separatorIndex <= 0)
        {
            continue;
        }

        var key = line[..separatorIndex].Trim();
        var value = line[(separatorIndex + 1)..].Trim().Trim('"');

        if (!string.IsNullOrWhiteSpace(key))
        {
            values[key] = value;
        }
    }

    if (values.Count > 0)
    {
        configuration.AddInMemoryCollection(values);
    }
}
