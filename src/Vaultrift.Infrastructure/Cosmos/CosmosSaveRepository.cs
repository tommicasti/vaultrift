using Vaultrift.Infrastructure.Persistence;

namespace Vaultrift.Infrastructure.Cosmos;

public sealed class CosmosSaveRepository : ISaveRepository
{
    private readonly IDocumentContainer _games;

    public CosmosSaveRepository(IDocumentContainer games)
    {
        ArgumentNullException.ThrowIfNull(games);
        _games = games;
    }

    public void Upsert(SaveRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        var key = DocumentKeys.Of(record.Id);
        var updatedAt = record.UpdatedAt.ToUniversalTime();
        var changes = new Dictionary<string, object?>
        {
            ["heroName"] = record.HeroName,
            ["floor"] = record.Floor,
            ["level"] = record.Level,
            ["data"] = record.Data,
            ["stats"] = SaveStats.From(record.Data),
            ["updatedAt"] = updatedAt
        };

        if (_games.Set(key, key, changes))
            return;

        if (!_games.Create(ToDocument(record, updatedAt), key))
            _games.Set(key, key, changes);
    }

    public SaveRecord? FindById(Guid id)
    {
        var key = DocumentKeys.Of(id);
        return ToRecord(_games.Read<GameDocument>(key, key));
    }

    public SaveRecord? FindByCode(string code)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        return ToRecord(_games.Query<GameDocument>(new DocumentQuery(DocumentTypes.Game, "code", code)).FirstOrDefault());
    }

    internal static GameDocument ToDocument(SaveRecord record, DateTimeOffset createdAt)
    {
        var key = DocumentKeys.Of(record.Id);
        return new GameDocument(
            key,
            key,
            DocumentTypes.Game,
            record.Code,
            record.ClassId,
            record.HeroName,
            record.Floor,
            record.Level,
            record.Data,
            SaveStats.From(record.Data),
            createdAt.ToUniversalTime(),
            record.UpdatedAt.ToUniversalTime());
    }

    private static SaveRecord? ToRecord(GameDocument? document) =>
        document is null
            ? null
            : new SaveRecord(
                Guid.Parse(document.Id),
                document.Code,
                document.ClassId,
                document.HeroName,
                document.Floor,
                document.Level,
                document.Data,
                document.UpdatedAt);
}
