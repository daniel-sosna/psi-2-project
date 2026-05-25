using System.Diagnostics.CodeAnalysis;
using KNOTS.Components;
using KNOTS.Services;
using KNOTS.Services.Interfaces;
using KNOTS.Data;
using KNOTS.Hubs;
using KNOTS.Models;
using KNOTS.Services.Chat;
using KNOTS.Services.Compability;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Razor Components
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

// Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=knots.db";
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString));

// Services
builder.Services.AddScoped<InterfaceLoggingService, LoggingService>(sp =>
    new LoggingService("logs"));

builder.Services.AddScoped<InterfaceSwipeRepository, SwipeRepository>();
builder.Services.AddScoped<InterfaceCompatibilityCalculator, CompatibilityCalculator>();
builder.Services.AddScoped<InterfaceUserService, UserService>();
builder.Services.AddScoped<IFriendService, FriendService>();
builder.Services.AddScoped<InterfaceCompatibilityService, CompatibilityService>();
builder.Services.AddSingleton<IGameRoomService, GameRoomService>();
builder.Services.AddSingleton<IUserIdProvider, NameUserIdProvider>();


// SignalR
builder.Services.AddSignalR();
builder.Services.AddScoped<IMessageService, MessageService>();
builder.Services.AddHostedService<FriendRequestCleanupBackgroundService>();

var app = builder.Build();

// Database + services initialization
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    try
    {
        dbContext.Database.Migrate();
        var tableCount = dbContext.Model.GetEntityTypes().Count();
        Console.WriteLine($"Database migrated successfully with {tableCount} tables");

        if (!app.Environment.IsEnvironment("Testing"))
        {
            var compatService = scope.ServiceProvider.GetRequiredService<InterfaceCompatibilityService>();
            Console.WriteLine("CompatibilityService initialized successfully");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Database initialization error: {ex.Message}");
        Console.WriteLine(ex.StackTrace);
        throw;
    }
}

// Error handling
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

// Routing
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
app.MapHub<GameHub>("/gamehub");
app.MapHub<ChatHub>("/chathub");
app.MapPost("/api/auth/business/register", (BusinessRegisterRequest request, InterfaceUserService userService) =>
{
    try
    {
        var result = userService.RegisterBusinessUser(request.BusinessName, request.Email, request.Password);
        return result.Success
            ? Results.Ok(new BusinessAuthResponse(true, result.Message, request.BusinessName, request.Email, true))
            : Results.BadRequest(new BusinessAuthResponse(false, result.Message, null, null, false));
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new BusinessAuthResponse(false, ex.Message, null, null, false));
    }
}).DisableAntiforgery();
app.MapPost("/api/auth/business/login", (BusinessLoginRequest request, InterfaceUserService userService) =>
{
    try
    {
        var result = userService.LoginBusinessUser(request.Email, request.Password);
        return result.Success
            ? Results.Ok(new BusinessAuthResponse(
                true,
                result.Message,
                userService.CurrentUser,
                userService.CurrentUserEmail,
                userService.IsCurrentUserBusiness))
            : Results.Unauthorized();
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new BusinessAuthResponse(false, ex.Message, null, null, false));
    }
}).DisableAntiforgery();
app.Run();
