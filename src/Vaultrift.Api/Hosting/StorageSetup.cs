using Vaultrift.Infrastructure.Persistence;

namespace Vaultrift.Api.Hosting;

public static class StorageSetup
{
    public const string ConnectionName = "Vaultrift";

    public static IServiceCollection AddGameStorage(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        var provider = configuration["Storage:Provider"] ?? "File";

        ISaveRepository repository = provider switch
        {
            "SqlServer" => new SqlSaveRepository(
                configuration.GetConnectionString(ConnectionName)
                ?? throw new InvalidOperationException($"Storage:Provider is SqlServer but ConnectionStrings:{ConnectionName} is missing.")),
            "File" => new FileSaveRepository(Path.Combine(environment.ContentRootPath, configuration["Storage:Folder"] ?? "saves")),
            "Memory" => new MemorySaveRepository(),
            _ => throw new InvalidOperationException($"Unknown Storage:Provider '{provider}'. Use SqlServer, File or Memory.")
        };

        services.AddSingleton<ISaveRepository>(provider =>
        {
            var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("Vaultrift.Saves");
            return new WriteBehindSaveRepository(
                repository,
                (error, record) => logger.LogError(error, "Could not save game {GameId} ({Code}); retrying.", record.Id, record.Code));
        });
        services.AddSingleton<IGameStore, CachedGameStore>();
        return services;
    }
}
