using System.Diagnostics;
using Microsoft.AspNetCore.SignalR;
using Vaultrift.Engine.Realtime;

namespace Vaultrift.Api.Realtime;

public sealed class RoomTicker(RoomRegistry rooms, IHubContext<RoomHub> hub, TimeProvider time, ILogger<RoomTicker> logger) : BackgroundService
{
    public const int MaxCatchUpTicks = 3;
    public const int PollsPerTick = 5;
    public const string IdleMessage = "The world closed after a long time without moves.";
    public static readonly TimeSpan IdleLimit = TimeSpan.FromMinutes(10);
    public static readonly TimeSpan IdlePoll = TimeSpan.FromMilliseconds(250);

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);

        foreach (var room in rooms.All)
            rooms.Close(room);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(LiveRules.TickSeconds / PollsPerTick), time);
        var clock = Stopwatch.StartNew();
        long done = 0;

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            var due = (long)(clock.Elapsed.TotalSeconds * LiveRules.TicksPerSecond);

            if (rooms.Count == 0)
            {
                done = due;
                await Task.Delay(IdlePoll, time, stoppingToken);
                continue;
            }

            var ticks = (int)Math.Min(due - done, MaxCatchUpTicks);
            done = due;

            if (ticks <= 0)
                continue;

            await AdvanceAll(ticks, stoppingToken);
        }
    }

    private async Task AdvanceAll(int ticks, CancellationToken stoppingToken)
    {
        var now = time.GetUtcNow();
        var sends = new List<Task>();

        if (rooms.ExpireAway(now) is { Count: > 0 } orphans)
            sends.Add(hub.Clients.Clients(orphans).SendAsync("Closed", RoomHub.HostLeftMessage, stoppingToken));

        foreach (var room in rooms.All)
        {
            if (now - room.LastInputAt > IdleLimit)
            {
                var connections = rooms.Close(room);
                sends.Add(hub.Clients.Clients(connections).SendAsync("Closed", IdleMessage, stoppingToken));
                continue;
            }

            foreach (var (connection, snapshot) in room.Advance(ticks))
                sends.Add(hub.Clients.Client(connection).SendAsync("Snapshot", snapshot, stoppingToken));

            if (room.EndedMessage is { } ended)
                sends.Add(hub.Clients.Clients(rooms.Close(room)).SendAsync("Closed", ended, stoppingToken));
        }

        try
        {
            await Task.WhenAll(sends);
        }
        catch (Exception error) when (error is not OperationCanceledException)
        {
            logger.LogWarning(error, "Could not deliver a live snapshot.");
        }
    }
}
