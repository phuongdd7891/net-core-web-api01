using WebApplication1.User.Services;
using CoreLibrary.DataModels;
using StackExchange.Redis.Extensions.Core.Configuration;
using StackExchange.Redis.Extensions.Newtonsoft;
using CoreLibrary.Repository;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

builder.Configuration.AddJsonFile($"./net-webapi01/WebApplication1.User/appsettings.{builder.Environment.EnvironmentName}.json", optional: true);

//db configs
var dbName = builder.Configuration.GetValue<string>("DatabaseName");
var dbConnectionStr = builder.Configuration.GetConnectionString("MongoDb");
services.AddIdentity<ApplicationUser, ApplicationRole>()
        .AddMongoDbStores<ApplicationUser, ApplicationRole, Guid>
        (
            dbConnectionStr, dbName
        );

var redisConfiguration = builder.Configuration.GetSection("Redis").Get<RedisConfiguration>();
services.AddStackExchangeRedisExtensions<NewtonsoftSerializer>(redisConfiguration!);

services.AddEndpointsApiExplorer(); 
services.AddSwaggerGen(); 

// Add services to the container.
services.AddTransient<RedisRepository>();
services.AddTransient<JwtService>();

var jwtSettings = builder.Configuration.GetSection("Jwt");
var tokenValidationParameters = new TokenValidationParameters
{
    ValidateIssuer = true,
    ValidateAudience = true,
    ValidateLifetime = true,
    ValidateIssuerSigningKey = true,
    ValidIssuer = jwtSettings["Issuer"],
    ValidAudience = jwtSettings["Audience"],
    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Secret"]!)),
};
services.AddSingleton(tokenValidationParameters);

services.AddGrpcSwagger();
services.AddGrpc();
services.AddGrpcReflection();

var app = builder.Build();

// Register services to the container.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapGrpcReflectionService();
}

app.MapGrpcService<AuthService>();
app.MapGrpcService<FileService>();

app.Run();
