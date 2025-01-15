using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using WebApi.Services;
using CoreLibrary.Repository;
using StackExchange.Redis.Extensions.Core.Configuration;
using StackExchange.Redis.Extensions.Newtonsoft;
using Newtonsoft.Json.Serialization;
using WebApi.Services.Grpc;
using WebApi.Data;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using CoreLibrary.DataModels;
using CoreLibrary.Models;

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

//db configs
var mongoDbSettings = builder.Configuration.GetSection("BookDatabase").Get<DbSettings>();
services.Configure<DbSettings>(builder.Configuration.GetSection("AdminDatabase"));
services.AddIdentity<ApplicationUser, ApplicationRole>()
        .AddMongoDbStores<ApplicationUser, ApplicationRole, Guid>
        (
            mongoDbSettings?.ConnectionString, mongoDbSettings?.DatabaseName
        );

var redisConfiguration = builder.Configuration.GetSection("Redis").Get<RedisConfiguration>();
services.AddStackExchangeRedisExtensions<NewtonsoftSerializer>(redisConfiguration!);

services.AddScoped(serviceProvider =>
{
    var settings = serviceProvider.GetRequiredService<IOptions<DbSettings>>().Value;
    return new AppDBContext(settings.ConnectionString, settings.DatabaseName);
});
services.AddTransient<RedisRepository>();
services.AddTransient<JwtService>();
services.AddTransient<AdminRepository>();
services.AddGrpc(options =>
{
    options.Interceptors.Add<JwtValidationInterceptor>();
});

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

var excludeMethods = new List<string>
{
    "/adminauthservice.AdminAuthServiceProto/Login"
};
services.AddTransient(sp => new JwtValidationInterceptor(tokenValidationParameters, excludeMethods));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseRouting();
app.MapControllers();

app.UseGrpcWeb();

app.MapGrpcService<AdminUserGrpcService>();
app.MapGrpcService<AuthGrpcService>();
app.UseRedisInformation();

app.Run();