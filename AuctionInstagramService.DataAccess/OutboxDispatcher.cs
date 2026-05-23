using AuctionInstagramService.Contracts;
using AuctionInstagramService.Database;
using AuctionInstagramService.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace AuctionInstagramService.DataAccess;

public sealed class OutboxDispatcher(
    IServiceScopeFactory scopeFactory,
    ILogger<OutboxDispatcher> logger
) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(500);
    private const int BatchSize = 64;

    private static string? ResolveChannel(string eventType, Guid aggregateId) => eventType switch
    {
        nameof(BidMadeEvent) => BidChannels.For(aggregateId),
        _ => null,
    };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DispatchBatchAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Outbox dispatch failed");
            }

            try { await Task.Delay(PollInterval, stoppingToken); }
            catch (OperationCanceledException) { return; }
        }
    }

    private async Task DispatchBatchAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AuctionDbContext>();
        var redis = scope.ServiceProvider.GetRequiredService<IConnectionMultiplexer>();

        var pending = await db.OutboxEvents
            .Where(e => e.ProcessedAt == null)
            .OrderBy(e => e.OccurredAt)
            .Take(BatchSize)
            .Select(e => new { e.Id, e.EventType, e.AggregateId, e.Payload })
            .ToListAsync(ct);

        if (pending.Count == 0) return;

        var subscriber = redis.GetSubscriber();
        foreach (var evt in pending)
        {
            var channel = ResolveChannel(evt.EventType, evt.AggregateId);
            if (channel is null)
            {
                logger.LogWarning("Unknown outbox event type '{EventType}' (id {Id}); marking processed to unblock queue.",
                    evt.EventType, evt.Id);
            }
            else
            {
                await subscriber.PublishAsync(RedisChannel.Literal(channel), evt.Payload);
            }

            await db.OutboxEvents
                .Where(e => e.Id == evt.Id)
                .ExecuteUpdateAsync(
                    set => set.SetProperty(e => e.ProcessedAt, DateTimeOffset.UtcNow),
                    ct);
        }
    }
}
