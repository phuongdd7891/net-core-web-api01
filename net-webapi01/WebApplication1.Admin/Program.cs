using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using WebApi.Models;
using WebApi.Services;
using CoreLibrary.Repository;
using StackExchange.Redis.Extensions.Core.Configuration;
using StackExchange.Redis.Extensions.Newtonsoft;
using Newtonsoft.Json.Serialization;
using WebApi.Services.Grpc;
using WebApi.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using CoreLibrary.Helpers;
using Microsoft.AspNetCore.Authorization;
using WebApi.Authentication;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

// Add services to the container.
services.AddControllers()
    .AddNewtonsoftJson(
        options => options.SerializerSettings.ContractResolver = new DefaultContractResolver
        {
            NamingStrategy = new CamelCaseNamingStrategy()
        });
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
services.AddEndpointsApiExplorer();
services.AddGrpcSwagger();
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
.AddScheme<JwtBearerOptions, SessionTokenAuthSchemeHandler>(
    "SessionTokens",
    opts => { }
);

services.AddAuthorization(options =>
{
    options.DefaultPolicy = new AuthorizationPolicyBuilder()
        //.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
        .AddAuthenticationSchemes("SessionTokens")
        .RequireAuthenticatedUser()
        .Build();
});

//db configs
var mongoDbSettings = builder.Configuration.GetSection("BookDatabase").Get<BookDatabaseSettings>();
services.Configure<BookDatabaseSettings>(builder.Configuration.GetSection("BookDatabase"));


var redisConfiguration = builder.Configuration.GetSection("Redis").Get<RedisConfiguration>();
services.AddStackExchangeRedisExtensions<NewtonsoftSerializer>(redisConfiguration!);

services.AddScoped(serviceProvider =>
{
    var settings = serviceProvider.GetRequiredService<IOptions<BookDatabaseSettings>>().Value;
    return new AppDBContext(settings.AdminConnectionString, settings.AdminDatabaseName);
});
services.AddTransient<RedisRepository>();
services.AddTransient<JwtService>();
services.AddTransient<AdminRepository>();
services.AddGrpc();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapGrpcService<AdminUserGrpcService>();
app.MapGrpcService<AuthGrpcService>();
app.UseRedisInformation();

app.Run();