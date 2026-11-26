using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Vaultrift.Api.Realtime;

namespace Vaultrift.Api.Hosting;

public static class RealtimeSetup
{
    public const string HubPath = "/hubs/room";

    public static IServiceCollection AddRealtime(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddSignalR()
            .AddJsonProtocol(options => options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton(new RoomRegistry(configuration.GetValue("Realtime:MaxRooms", RoomRegistry.DefaultMaxRooms)));
        services.AddHostedService<RoomTicker>();
        return services;
    }

    public static WebApplication MapRealtime(this WebApplication app)
    {
        app.MapHub<RoomHub>(HubPath);
        return app;
    }
}
