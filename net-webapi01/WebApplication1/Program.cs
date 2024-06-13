using System.Text;
using WebApi.Models;
using WebApi.Services;
using CoreLibrary.Repository;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using StackExchange.Redis.Extensions.Core.Configuration;
using StackExchange.Redis.Extensions.Newtonsoft;
using WebApplication1.Authentication;
using CoreLibrary.DbAccess;
using NLog.Extensions.Logging;
using NLog;
using Newtonsoft.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using CoreLibrary.Helpers;
using Microsoft.AspNetCore.Authorization;
using Serilog;
using Serilog.Sinks.Elasticsearch;
using System.Reflection;
using Serilog.Exceptions;
using WebApplication1.Middlewares;
using WebApplication1.SSE;

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
    options.AddSecurityDefinition("Username", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Query,
        Name = "u",
        Description = "Username",
        Type = SecuritySchemeType.ApiKey
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
        },
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Id = "Username",
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
services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
{
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
services.AddScoped(serviceProvider =>
{
    var settings = serviceProvider.GetRequiredService<IOptions<BookDatabaseSettings>>().Value;
    return new AppDBContext(settings.ConnectionString, settings.DatabaseName);
});
services.AddScoped(serviceProvider =>
{
    var settings = serviceProvider.GetRequiredService<IOptions<BookDatabaseSettings>>().Value;
    return new MongoDbContext(settings.ConnectionString, settings.DatabaseName);
});
services.AddScoped(serviceProvider =>
{
    var settings = serviceProvider.GetRequiredService<IOptions<BookDatabaseSettings>>().Value;
    return new AppAdminDBContext(settings.AdminConnectionString, settings.AdminDatabaseName);
});
services.AddScoped<IConnectionThrottlingPipeline>(serviceProvider =>
{
    var dbCtx = serviceProvider.GetRequiredService<MongoDbContext>();
    return new ConnectionThrottlingPipeline(dbCtx.mongoClient);
});
services.AddTransient<AdminService>();
services.AddTransient<BooksService>();
services.AddTransient<JwtService>();
services.AddTransient<ApiKeyService>();
services.AddTransient<RedisRepository>();
services.AddTransient<CacheService>();
services.AddTransient<RoleActionRepository>();
services.AddHostedService<InitializeCacheService>();
services.AddSingleton<ISseHolder, SseHolder>();

services.Configure<ApiBehaviorOptions>(opt =>
{
    opt.SuppressModelStateInvalidFilter = true;
});

// authentication
services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, x =>
        {
            x.RequireHttpsMetadata = false;
            x.SaveToken = true;
            x.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                ValidateIssuer = true,
                ValidateAudience = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(AESHelpers.Decrypt(builder.Configuration["Jwt:Secret"]!))),
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

services.AddAuthorization(options =>
{
    options.DefaultPolicy = new AuthorizationPolicyBuilder()
        .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
        .RequireAuthenticatedUser()
        .Build();
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

#region Serilog config
var environment = builder.Environment.EnvironmentName;
var logger = new LoggerConfiguration()
        .Enrich.FromLogContext()
        .Enrich.WithExceptionDetails()
        .Enrich.WithMachineName()
        .WriteTo.Debug()
        .WriteTo.Console()
        .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri(builder.Configuration["ElasticConfiguration:Uri"]!))
        {
            AutoRegisterTemplate = true,
            IndexFormat = $"{Assembly.GetExecutingAssembly().GetName().Name!.ToLower().Replace(".", "-")}-{environment.ToLower().Replace(".", "-")}-{DateTime.UtcNow:yyyy-MM}"
        })
        .Enrich.WithProperty("Environment", environment)
        .ReadFrom.Configuration(config)
        .CreateLogger();
builder.Host.UseSerilog(logger);
#endregion

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();
app.UseHttpsRedirection();
app.UseCors(MyAllowSpecificOrigins);
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.UseRedisInformation();
app.UseErrorHandling();
app.MapSseHolder("/sse/connect");
app.Lifetime.ApplicationStopped.Register(LogManager.Shutdown);
AppSettingsHelper.ConfigureSetting(app.Services.GetRequiredService<IConfiguration>());

app.Run();