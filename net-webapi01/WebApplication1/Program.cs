using System.Text;
using WebApi.Models;
using WebApi.Services;
using CoreLibrary.Repository;
using IdentityMongo.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using StackExchange.Redis.Extensions.Core.Configuration;
using StackExchange.Redis.Extensions.Newtonsoft;
using WebApplication1.Authentication;
using CoreLibrary.DbContext;
using NLog.Extensions.Logging;
using NLog;
using Newtonsoft.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using WebApi.Hubs;
using Microsoft.AspNetCore.Identity;

var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
services.AddEndpointsApiExplorer();
services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Name = "Authorization",
        Description = "Bearer Authentication with JWT Token",
        Type = SecuritySchemeType.Http
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Id = "Bearer",
                    Type = ReferenceType.SecurityScheme
                }
            },
            new List<string>()
        }
    });
});
services.AddCors(options =>
{
    options.AddPolicy(MyAllowSpecificOrigins,
                          policy =>
                          {
                              policy.WithOrigins("http://192.168.156.58:8089", "http://localhost:8089")
                                                  .AllowAnyHeader()
                                                  .AllowAnyMethod();
                          });
});
services.AddControllers()
    .AddNewtonsoftJson(
        options => options.SerializerSettings.ContractResolver = new DefaultContractResolver
        {
            NamingStrategy = new CamelCaseNamingStrategy()
        });

//db configs
var mongoDbSettings = builder.Configuration.GetSection("BookDatabase").Get<BookDatabaseSettings>();
services.Configure<BookDatabaseSettings>(builder.Configuration.GetSection("BookDatabase"));
services.AddIdentity<ApplicationUser, ApplicationRole>(options => {
            options.Password.RequireDigit = false;
            options.Password.RequireLowercase = false;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = false;
            options.Password.RequiredLength = 6;
        })
        .AddDefaultTokenProviders()
        .AddMongoDbStores<ApplicationUser, ApplicationRole, Guid>
        (
            mongoDbSettings?.ConnectionString, mongoDbSettings?.DatabaseName
        );

//redis
var redisConfiguration = builder.Configuration.GetSection("Redis").Get<RedisConfiguration>();
services.AddStackExchangeRedisExtensions<NewtonsoftSerializer>(redisConfiguration!);

// services
services.AddSingleton(serviceProvider =>
{
    var settings = serviceProvider.GetRequiredService<IOptions<BookDatabaseSettings>>().Value;
    return new AppDBContext(settings.ConnectionString, settings.DatabaseName);
});
services.AddSingleton(serviceProvider =>
{
    var settings = serviceProvider.GetRequiredService<IOptions<BookDatabaseSettings>>().Value;
    return new MongoDbContext(settings.ConnectionString, settings.DatabaseName);
});
services.AddSingleton(serviceProvider =>
{
    var settings = serviceProvider.GetRequiredService<IOptions<BookDatabaseSettings>>().Value;
    return new AppAdminDBContext(settings.AdminConnectionString, settings.AdminDatabaseName);
});

services.AddSingleton<BooksService>();
services.AddSingleton<JwtService>();
services.AddSingleton<ApiKeyService>();
services.AddSingleton<RedisRepository>();
services.AddSingleton<CacheService>();
services.AddSingleton<RoleActionRepository>();
services.AddHostedService<InitializeCacheService>();

services.Configure<ApiBehaviorOptions>(opt =>
{
    opt.SuppressModelStateInvalidFilter = true;
});

// authentication
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, x =>
        {
            x.RequireHttpsMetadata = false;
            x.SaveToken = true;
            x.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                ValidateIssuer = true,
                ValidateAudience = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(builder.Configuration["Jwt:Secret"]!)),
                ValidIssuer = builder.Configuration["Jwt:Issuer"],
                ValidAudience = builder.Configuration["Jwt:Audience"],
                ClockSkew = TimeSpan.Zero,
                ValidateLifetime = true
            };
        })
        .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(
            ApiKeyAuthenticationHandler.API_KEY_HEADER,
            options => { }
        );

services.AddSignalR((options) =>
        {
            options.EnableDetailedErrors = true;
            options.KeepAliveInterval = TimeSpan.FromSeconds(5);
        }).AddHubOptions<UserNotifications>((options) =>
        {
            options.KeepAliveInterval = TimeSpan.FromSeconds(5);
            options.EnableDetailedErrors = true;
        });

services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
services.AddSingleton<IEmailSender, EmailService>();

#region Nlog config
var config = new ConfigurationBuilder()
   .SetBasePath(Directory.GetCurrentDirectory())
   .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
   .Build();

NLog.LogManager.Configuration = new NLogLoggingConfiguration(config.GetSection("NLog"));
#endregion

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.MapControllers();
app.UseRedisInformation();
app.UseErrorHandling();
app.UseCors(MyAllowSpecificOrigins);
app.MapHub<UserNotifications>("/notifications");
app.Lifetime.ApplicationStopped.Register(LogManager.Shutdown);
AppSettingsHelper.ConfigureSetting(app.Services.GetRequiredService<IConfiguration>());

app.Run();