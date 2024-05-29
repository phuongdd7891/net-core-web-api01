using AdminWeb;
using AdminWeb.Handler;
using AdminWeb.Handlers;
using AdminWeb.Middlewares;
using AdminWeb.Services;
using AspNetCore.Unobtrusive.Ajax;
using AspNetCoreHero.ToastNotification;
using AspNetCoreHero.ToastNotification.Extensions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddSession();
builder.Services.AddHttpContextAccessor();

//builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddTransient<AuthorizationRequestHandler>();
builder.Services.AddTransient<ProfileService>();
builder.Services.AddHttpClient<OperationService>()
    .AddHttpMessageHandler<AuthorizationRequestHandler>();

builder.Services.AddAuthentication("SessionTokens")
   .AddScheme<AuthenticationSchemeOptions, SessionTokenAuthSchemeHandler>(
       "SessionTokens",
       opts => { }
   );
builder.Services.AddAuthorization(options =>
{
    options.DefaultPolicy = new AuthorizationPolicyBuilder()
        .AddAuthenticationSchemes("SessionTokens")
        .RequireAuthenticatedUser()
        .Build();
});

builder.Services.AddNotyf(config =>
{
    config.DurationInSeconds = 5;
    config.IsDismissable = true;
    config.Position = NotyfPosition.TopCenter;
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<ToastMessageService>();
builder.Services.AddUnobtrusiveAjax();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseSession();

app.UseRouting();
//app.UseExceptionHandler(_ => { });
app.UseMiddleware<ErrorHandlingMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.UseNotyf();
app.UseUnobtrusiveAjax();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();
