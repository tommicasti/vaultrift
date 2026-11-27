using System.Collections.Concurrent;

namespace Vaultrift.Infrastructure.Vault;

public sealed class WriteBehindVaultRepository : IVaultRepository, IDisposable
{
    private static readonly TimeSpan DefaultRetryDelay = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan ShutdownWait = TimeSpan.FromSeconds(10);

    private readonly IVaultRepository _inner;
    private readonly Action<Exception, VaultKind, Guid>? _onError;
    private readonly TimeSpan _retryDelay;
    private readonly ConcurrentDictionary<(Guid AccountId, VaultKind Kind, Guid Id), Pending> _pending = new();
    private readonly SemaphoreSlim _signal = new(0);
    private readonly CancellationTokenSource _stop = new();
    private readonly Task _worker;
    private bool _disposed;

    private sealed record Pending(VaultEntry? Entry);

    public WriteBehindVaultRepository(IVaultRepository inner, Action<Exception, VaultKind, Guid>? onError = null, TimeSpan? retryDelay = null)
    {
        ArgumentNullException.ThrowIfNull(inner);

        _inner = inner;
        _onError = onError;
        _retryDelay = retryDelay ?? DefaultRetryDelay;
        _worker = Task.Run(WorkAsync);
    }

    public int PendingCount => _pending.Count;

    public IReadOnlyList<VaultEntry> List(Guid accountId)
    {
        var pending = _pending.Where(pair => pair.Key.AccountId == accountId).ToDictionary(pair => (pair.Key.Kind, pair.Key.Id), pair => pair.Value.Entry);
        var stored = _inner.List(accountId).Where(entry => !pending.ContainsKey((entry.Kind, entry.Id)));

        return stored
            .Concat(pending.Values.OfType<VaultEntry>())
            .OrderByDescending(entry => entry.UpdatedAt)
            .ToList();
    }

    public VaultEntry? Find(Guid accountId, VaultKind kind, Guid id) =>
        _pending.TryGetValue((accountId, kind, id), out var pending) ? pending.Entry : _inner.Find(accountId, kind, id);

    public void Upsert(VaultEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        Queue((entry.AccountId, entry.Kind, entry.Id), new Pending(entry));
    }

    public bool Delete(Guid accountId, VaultKind kind, Guid id)
    {
        if (Find(accountId, kind, id) is null)
            return false;

        Queue((accountId, kind, id), new Pending(null));
        return true;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _stop.Cancel();

        try
        {
            _worker.Wait(ShutdownWait);
        }
        catch (AggregateException)
        {
        }

        WritePending(retryLater: false);
        _stop.Dispose();
        _signal.Dispose();
    }

    private void Queue((Guid, VaultKind, Guid) key, Pending pending)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _pending[key] = pending;
        _signal.Release();
    }

    private async Task WorkAsync()
    {
        while (!_stop.IsCancellationRequested)
        {
            try
            {
                await _signal.WaitAsync(_stop.Token);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            WritePending(retryLater: true);
        }
    }

    private void WritePending(bool retryLater)
    {
        foreach (var pending in _pending.ToArray())
        {
            try
            {
                if (pending.Value.Entry is { } entry)
                    _inner.Upsert(entry);
                else
                    _inner.Delete(pending.Key.AccountId, pending.Key.Kind, pending.Key.Id);
            }
            catch (Exception error)
            {
                _onError?.Invoke(error, pending.Key.Kind, pending.Key.Id);

                if (retryLater)
                    ScheduleRetry();

                return;
            }

            _pending.TryRemove(pending);
        }
    }

    private void ScheduleRetry() =>
        _ = Task.Delay(_retryDelay, _stop.Token).ContinueWith(
            _ => _signal.Release(),
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnRanToCompletion,
            TaskScheduler.Default);
}
