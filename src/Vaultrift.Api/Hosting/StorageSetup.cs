using Vaultrift.Infrastructure.Cosmos;
using Vaultrift.Infrastructure.Persistence;
using Vaultrift.Infrastructure.Vault;

namespace Vaultrift.Api.Hosting;

public static class StorageSetup
{
    public const string ConnectionName = "Vaultrift";
    public const string CosmosConnectionName = "Cosmos";

    public static IServiceCollection AddGameStorage(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        var provider = configuration["Storage:Provider"] ?? "File";

        if (provider is not ("Cosmos" or "SqlServer" or "File" or "Memory"))
            throw new InvalidOperationException($"Unknown Storage:Provider '{provider}'. Use Cosmos, SqlServer, File or Memory.");

        if (provider == "Cosmos")
            services.AddSingleton(serviceProvider => OpenCosmos(configuration, serviceProvider));

        services.AddSingleton<ISaveRepository>(serviceProvider =>
        {
            ISaveRepository repository = provider switch
            {
                "Cosmos" => new CosmosSaveRepository(serviceProvider.GetRequiredService<CosmosStore>().Games),
                "SqlServer" => new SqlSaveRepository(
                    configuration.GetConnectionString(ConnectionName)
                    ?? throw new InvalidOperationException($"Storage:Provider is SqlServer but ConnectionStrings:{ConnectionName} is missing.")),
                "File" => new FileSaveRepository(Path.Combine(environment.ContentRootPath, configuration["Storage:Folder"] ?? "saves")),
                _ => new MemorySaveRepository()
            };

            var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Vaultrift.Saves");
            return new WriteBehindSaveRepository(
                repository,
                (error, record) => logger.LogError(error, "Could not save game {GameId} ({Code}); retrying.", record.Id, record.Code));
        });
        services.AddSingleton<IGameStore, CachedGameStore>();
        services.AddSingleton<IVaultRepository>(serviceProvider =>
        {
            IVaultRepository repository = provider switch
            {
                "Cosmos" => new CosmosVaultRepository(serviceProvider.GetRequiredService<CosmosStore>().Vault),
                "SqlServer" => new SqlVaultRepository(configuration.GetConnectionString(ConnectionName)!),
                "File" => new FileVaultRepository(Path.Combine(environment.ContentRootPath, configuration["Storage:Folder"] ?? "saves")),
                _ => new MemoryVaultRepository()
            };

            var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Vaultrift.Vault");
            return new WriteBehindVaultRepository(
                repository,
                (error, kind, id) => logger.LogError(error, "Could not save {Kind} {Id}; retrying.", kind, id));
        });
        return services;
    }

    private static CosmosStore OpenCosmos(IConfiguration configuration, IServiceProvider serviceProvider)
    {
        var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Vaultrift.Cosmos");

        return CosmosStore.Open(
            configuration.GetConnectionString(CosmosConnectionName)
            ?? throw new InvalidOperationException($"Storage:Provider is Cosmos but ConnectionStrings:{CosmosConnectionName} is missing."),
            configuration["Storage:Database"],
            logger.IsEnabled(LogLevel.Debug) ? (operation, units) => logger.LogDebug("{Operation} cost {RequestUnits} RU", operation, units) : null);
    }
}
