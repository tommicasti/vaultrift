using System.Collections.Concurrent;
using Vaultrift.Infrastructure.Content;

namespace Vaultrift.Infrastructure.Persistence;

public sealed class CachedGameStore : IGameStore
{
    private const int MaxCodeAttempts = 10;

    private static readonly TimeSpan IdleLimit = TimeSpan.FromHours(2);

    private readonly ConcurrentDictionary<Guid, GameSession> _sessions = new();
    private readonly ISaveRepository _repository;

    public CachedGameStore(ISaveRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        _repository = repository;
    }

    public int CachedCount => _sessions.Count;

    public GameSession Create(ClassDefinition playerClass, string? playerName)
    {
        ArgumentNullException.ThrowIfNull(playerClass);

        var session = GameSession.Start(Guid.NewGuid(), FreeCode(), playerClass, playerName);
        _repository.Upsert(session.ToRecord());
        Remember(session);
        return session;
    }

    public GameSession? Find(Guid id)
    {
        if (_sessions.TryGetValue(id, out var cached))
        {
            cached.Touch();
            return cached;
        }

        return Load(_repository.FindById(id));
    }

    public GameSession? FindByCode(string code)
    {
        var normalized = SaveCode.Normalize(code);

        if (normalized is null)
            return null;

        var cached = _sessions.Values.FirstOrDefault(session => session.Code == normalized);

        if (cached is not null)
        {
            cached.Touch();
            return cached;
        }

        return Load(_repository.FindByCode(normalized));
    }

    public void Save(GameSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        session.Touch();
        _repository.Upsert(session.ToRecord());
    }

    private GameSession? Load(SaveRecord? record)
    {
        if (record is null)
            return null;

        var session = GameSession.Resume(record.Id, record.Code, SaveFile.FromJson(record.Data));
        return _sessions.GetOrAdd(session.Id, session);
    }

    private void Remember(GameSession session)
    {
        EvictIdle();
        _sessions[session.Id] = session;
    }

    private void EvictIdle()
    {
        var cutoff = DateTimeOffset.UtcNow - IdleLimit;

        foreach (var (id, session) in _sessions)
        {
            if (session.LastUsed < cutoff)
                _sessions.TryRemove(id, out _);
        }
    }

    private string FreeCode()
    {
        for (var attempt = 0; attempt < MaxCodeAttempts; attempt++)
        {
            var code = SaveCode.Generate();

            if (_repository.FindByCode(code) is null)
                return code;
        }

        throw new InvalidOperationException("Could not find a free save code.");
    }
}
