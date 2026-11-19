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

        services.AddSingleton(repository);
        services.AddSingleton<IGameStore, CachedGameStore>();
        return services;
    }
}
