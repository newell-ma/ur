using RoyalGameOfUr.Server.Hubs;
using RoyalGameOfUr.Server.Rooms;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR();
builder.Services.AddSingleton<RoomManager>();

var app = builder.Build();

app.UseStaticFiles();
app.UseBlazorFrameworkFiles();
app.UseRouting();

app.MapHub<GameHub>("/gamehub");
app.MapFallbackToFile("index.html");

app.Run();
