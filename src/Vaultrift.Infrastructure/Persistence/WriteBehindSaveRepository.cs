using System.Collections.Concurrent;

namespace Vaultrift.Infrastructure.Persistence;

public sealed class WriteBehindSaveRepository : ISaveRepository, IDisposable
{
    private static readonly TimeSpan DefaultRetryDelay = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan ShutdownWait = TimeSpan.FromSeconds(10);

    private readonly ISaveRepository _inner;
    private readonly Action<Exception, SaveRecord>? _onError;
    private readonly TimeSpan _retryDelay;
    private readonly ConcurrentDictionary<Guid, SaveRecord> _pending = new();
    private readonly SemaphoreSlim _signal = new(0);
    private readonly CancellationTokenSource _stop = new();
    private readonly Task _worker;
    private bool _disposed;

    public WriteBehindSaveRepository(ISaveRepository inner, Action<Exception, SaveRecord>? onError = null, TimeSpan? retryDelay = null)
    {
        ArgumentNullException.ThrowIfNull(inner);

        _inner = inner;
        _onError = onError;
        _retryDelay = retryDelay ?? DefaultRetryDelay;
        _worker = Task.Run(WorkAsync);
    }

    public int PendingCount => _pending.Count;

    public void Upsert(SaveRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        ObjectDisposedException.ThrowIf(_disposed, this);

        _pending[record.Id] = record;
        _signal.Release();
    }

    public SaveRecord? FindById(Guid id) =>
        _pending.TryGetValue(id, out var record) ? record : _inner.FindById(id);

    public SaveRecord? FindByCode(string code) =>
        _pending.Values.FirstOrDefault(record => record.Code == code) ?? _inner.FindByCode(code);

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
                _inner.Upsert(pending.Value);
            }
            catch (Exception error)
            {
                _onError?.Invoke(error, pending.Value);

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
