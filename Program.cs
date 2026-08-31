using System.Text;
using System.Text.Json.Serialization;
using EdgeTech.API.Data;
using EdgeTech.API.Models;
using EdgeTech.API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;

// 1. Configure MongoDB Global Conventions & ClassMaps before any Mongo operations
var conventionPack = new ConventionPack
{
    new IgnoreExtraElementsConvention(true),
    new IgnoreIfNullConvention(true)
};
ConventionRegistry.Register("GlobalConventions", conventionPack, t => true);

foreach (var type in typeof(Order).Assembly.GetTypes().Where(t => t.Namespace == "EdgeTech.API.Models" && t.IsClass))
{
    if (!BsonClassMap.IsClassMapRegistered(type))
    {
        var classMap = new BsonClassMap(type);
        classMap.AutoMap();
        classMap.SetIgnoreExtraElements(true);
        BsonClassMap.RegisterClassMap(classMap);
    }
}

var builder = WebApplication.CreateBuilder(args);

// Mongo
builder.Services.AddSingleton<MongoDbContext>();

// JWT
var jwtSecret = builder.Configuration["Jwt:Secret"] ?? "EdgeTechSuperSecretKeyMustBe32CharactersLong!";
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
        ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "EdgeTechAPI",
        ValidAudience = builder.Configuration["Jwt:Audience"] ?? "EdgeTechClient",
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
    };
});

builder.Services.AddAuthorization();

// Services
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IBlobStorageService, BlobStorageService>();
builder.Services.AddScoped<IIdGeneratorService, IdGeneratorService>();

// CORS
var configuredOrigins = (builder.Configuration["Frontend:Url"] ?? "http://localhost:3000,http://localhost:3001,https://edgetech.com.bd,https://www.edgetech.com.bd")
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.SetIsOriginAllowed(origin =>
        {
            if (string.IsNullOrWhiteSpace(origin)) return false;
            try
            {
                var uri = new Uri(origin);
                return uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
                       uri.Host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase) ||
                       uri.Host.Equals("edgetech.com.bd", StringComparison.OrdinalIgnoreCase) ||
                       uri.Host.EndsWith(".edgetech.com.bd", StringComparison.OrdinalIgnoreCase) ||
                       uri.Host.EndsWith(".vercel.app", StringComparison.OrdinalIgnoreCase) ||
                       configuredOrigins.Any(o => o.TrimEnd('/').Equals(origin.TrimEnd('/'), StringComparison.OrdinalIgnoreCase));
            }
            catch
            {
                return false;
            }
        })
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials();
    });
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "EdgeTech API",
        Version = "v1",
        Description = "REST API for EdgeTech ecommerce workflows: auth, catalog, cart, orders, package builder, and admin operations."
    });
    c.SupportNonNullableReferenceTypes();
    c.UseInlineDefinitionsForEnums();

    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
    }

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme. Example: 'Bearer {token}'",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Initialize Mongo collections/indexes/seed on startup.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MongoDbContext>();
    await MongoDbInitializer.InitializeAsync(db);
}

app.UseSwagger();
app.UseSwaggerUI();

// Behind reverse proxies like Cloudflare, SSL is terminated at the edge.
// app.UseHttpsRedirection() is omitted to prevent 301 redirect loops.
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
