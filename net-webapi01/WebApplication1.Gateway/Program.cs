using Gateway.Interceptors;
using Gateway.Middlewares;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Serialization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Text;
using Microsoft.Extensions.Primitives;
using StackExchange.Redis.Extensions.Core.Configuration;
using StackExchange.Redis.Extensions.Newtonsoft;
using CoreLibrary.Repository;
using Gateway.Services;
using Gateway.Models;
using Serilog;
using Microsoft.AspNetCore.Authentication;
using Gateway.Authorization;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

builder.Configuration
           .SetBasePath(Directory.GetCurrentDirectory())
           .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
           .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
           .AddEnvironmentVariables();

builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration.ReadFrom.Configuration(context.Configuration);
});
// Add services to the container.
services.AddControllers()
    .AddNewtonsoftJson(
        options => options.SerializerSettings.ContractResolver = new DefaultContractResolver
        {
            NamingStrategy = new CamelCaseNamingStrategy()
        });
services.AddEndpointsApiExplorer();
services.AddSwaggerGen();
services.Configure<ApiBehaviorOptions>(opt =>
{
    opt.SuppressModelStateInvalidFilter = true;
});

services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, x =>
{
    var jwtSettings = builder.Configuration.GetSection("Jwt");
    x.RequireHttpsMetadata = true;
    x.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!)),
    };
    x.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            // var authVal = StringValues.Empty;
            // if (context.HttpContext.Request.Headers.TryGetValue("Authorization", out authVal))
            // {
            //     context.HttpContext.RequestServices
            //     .GetRequiredService<ILogger<Program>>()
            //     .LogInformation("JWT received in request {0}: {1}", context.HttpContext.Request.Path, authVal.ToString());
            // }
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            // var logger = context.HttpContext.RequestServices
            //     .GetRequiredService<ILogger<Program>>();

            // var claims = context.Principal?.Claims.Select(c => new { c.Type, c.Value });
            //logger.LogInformation("JWT validated. Claims: {Claims}", claims);

            return Task.CompletedTask;
        },
        OnAuthenticationFailed = context =>
        {
            context.HttpContext.RequestServices
                .GetRequiredService<ILogger<Program>>()
                .LogError(context.Exception, "JWT authentication failed.");

            return Task.CompletedTask;
        }
    };
});

services.AddAuthorization(options =>
{
    options.DefaultPolicy = new AuthorizationPolicyBuilder()
        .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
        .RequireAuthenticatedUser()
        .Build();
});

var redisConfiguration = builder.Configuration.GetSection("Redis").Get<RedisConfiguration>();
services.AddStackExchangeRedisExtensions<NewtonsoftSerializer>(redisConfiguration!);

services.Configure<UploadSettings>(builder.Configuration.GetSection("UploadSettings"));

services.AddHttpContextAccessor();
services.AddScoped<ErrorHandlerInterceptor>();
services.AddTransient<RedisRepository>();
services.AddTransient<CacheService>();
services.AddScoped<IUserIdentity, UserIdentity>();

services.AddGrpc();
services.AddGrpcClients(builder.Configuration);

services.AddHostedService<InitializeCacheService>();
services.AddHostedService<FileHostedService>();

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
app.UseErrorHandling();

app.Run();
