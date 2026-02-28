using RoyalGameOfUr.Server.Hubs;
using RoyalGameOfUr.Server.Rooms;
using RoyalGameOfUr.Server.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR();
builder.Services.AddSingleton<RoomManager>();
builder.Services.AddSingleton<IGameBroadcaster, SignalRGameBroadcaster>();
builder.Services.AddSingleton<IRoomService, RoomService>();

var app = builder.Build();

app.UseStaticFiles();
app.UseBlazorFrameworkFiles();
app.UseRouting();

app.MapHub<GameHub>("/gamehub");
app.MapFallbackToFile("index.html");

app.Run();
