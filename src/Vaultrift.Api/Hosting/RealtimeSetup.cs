using System.Net.WebSockets;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Vaultrift.Api.Realtime;
using Vaultrift.Infrastructure.Accounts;
using Vaultrift.Infrastructure.Vault;

namespace Vaultrift.Api.Hosting;

public static class RealtimeSetup
{
    public const string HubPath = "/hubs/room";
    public static readonly TimeSpan KeepAlive = TimeSpan.FromSeconds(5);
    public static readonly TimeSpan ClientTimeout = TimeSpan.FromSeconds(15);

    public static IServiceCollection AddRealtime(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddSignalR(options =>
            {
                options.KeepAliveInterval = KeepAlive;
                options.ClientTimeoutInterval = ClientTimeout;
            })
            .AddJsonProtocol(options =>
            {
                options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                options.PayloadSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            });

        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton(provider => new RoomRegistry(
            configuration.GetValue("Realtime:MaxRooms", RoomRegistry.DefaultMaxRooms),
            provider.GetRequiredService<VaultService>(),
            provider.GetRequiredService<TrophyService>().RecordLive));
        services.AddHostedService<RoomTicker>();
        return services;
    }

    public static WebApplication MapRealtime(this WebApplication app)
    {
        app.UseWebSockets();
        app.Use((context, next) =>
        {
            if (context.Request.Path.StartsWithSegments(HubPath) && context.Features.Get<IHttpWebSocketFeature>() is { IsWebSocketRequest: true } sockets)
                context.Features.Set<IHttpWebSocketFeature>(new CompressedWebSockets(sockets));
            return next(context);
        });
        app.MapHub<RoomHub>(HubPath);
        return app;
    }

    private sealed class CompressedWebSockets(IHttpWebSocketFeature inner) : IHttpWebSocketFeature
    {
        public bool IsWebSocketRequest => inner.IsWebSocketRequest;

        public Task<WebSocket> AcceptAsync(WebSocketAcceptContext context)
        {
            context.DangerousEnableCompression = true;
            return inner.AcceptAsync(context);
        }
    }
}
