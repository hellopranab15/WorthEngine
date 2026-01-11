using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using WorthEngine.Core.Interfaces;
using WorthEngine.Services;
using WorthEngine.Services.Repositories;

var builder = WebApplication.CreateBuilder(args);

// MongoDB Configuration
var mongoConnectionString = builder.Configuration.GetConnectionString("MongoDB") ?? "mongodb://localhost:27017";
var mongoDatabaseName = builder.Configuration["MongoDB:DatabaseName"] ?? "worthengine";
var mongoClient = new MongoClient(mongoConnectionString);
var mongoDatabase = mongoClient.GetDatabase(mongoDatabaseName);
builder.Services.AddSingleton(mongoDatabase);

// JWT Configuration
var jwtKey = builder.Configuration["Jwt:Key"] ?? "WorthEngineDefaultSecretKeyForDevelopment2024!";
var key = Encoding.UTF8.GetBytes(jwtKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "WorthEngine",
        ValidateAudience = true,
        ValidAudience = builder.Configuration["Jwt:Audience"] ?? "WorthEngineApp",
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

// Repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IPortfolioRepository, PortfolioRepository>();
builder.Services.AddScoped<IInsuranceRepository, InsuranceRepository>();
builder.Services.AddScoped<IMutualFundRepository, MutualFundRepository>();

// Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IPortfolioService, PortfolioService>();
builder.Services.AddScoped<IInsuranceService, InsuranceService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IXirrService, XirrService>();

// HTTP Client for Market Data Service
builder.Services.AddHttpClient<IMarketDataService, MarketDataService>();

// Background Service
builder.Services.AddHostedService<PriceRefreshBackgroundService>();

// Controllers and Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Worth Engine API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using Bearer scheme. Enter 'Bearer' [space] and then your token.",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// CORS for Angular frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularDev", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAngularDev");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
