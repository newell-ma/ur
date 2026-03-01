using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using RoyalGameOfUr.Server.Hubs;
using RoyalGameOfUr.Server.Rooms;
using RoyalGameOfUr.Server.Services;

namespace RoyalGameOfUr.E2E.Infrastructure;

public sealed class TestServerHost : IAsyncDisposable
{
    private WebApplication? _app;
    private readonly Action<IServiceCollection>? _configureServices;

    public string BaseUrl { get; private set; } = "";

    public TestServerHost(Action<IServiceCollection>? configureServices = null)
    {
        _configureServices = configureServices;
    }

    public async Task StartAsync()
    {
        var serverProjectDir = FindProjectDirectory("src/RoyalGameOfUr.Server");

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ContentRootPath = serverProjectDir,
            // ApplicationName must match the server assembly so StaticWebAssetsLoader
            // can find the .staticwebassets.runtime.json manifest in the bin directory.
            ApplicationName = typeof(GameHub).Assembly.GetName().Name!,
            EnvironmentName = "Development"
        });

        builder.WebHost.UseUrls("http://127.0.0.1:0");

        builder.Services.AddSignalR();
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddSingleton<RoomManager>();
        builder.Services.AddSingleton<IGameBroadcaster, SignalRGameBroadcaster>();
        builder.Services.AddSingleton<IRoomService, RoomService>();

        _configureServices?.Invoke(builder.Services);

        _app = builder.Build();

        _app.UseStaticFiles();
        _app.UseBlazorFrameworkFiles();
        _app.UseRouting();

        _app.MapHub<GameHub>("/gamehub");
        _app.MapFallbackToFile("index.html");

        await _app.StartAsync();

        BaseUrl = _app.Urls.First();
    }

    public async ValueTask DisposeAsync()
    {
        if (_app is not null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }

    private static string FindProjectDirectory(string relativePath)
    {
        var dir = AppContext.BaseDirectory;
        while (dir is not null)
        {
            var candidate = Path.Combine(dir, relativePath);
            if (Directory.Exists(candidate))
                return Path.GetFullPath(candidate);
            dir = Path.GetDirectoryName(dir);
        }

        throw new InvalidOperationException(
            $"Could not find '{relativePath}' relative to '{AppContext.BaseDirectory}'. " +
            "Ensure the solution is built before running E2E tests.");
    }
}
