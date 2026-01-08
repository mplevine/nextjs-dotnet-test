using System.Collections.Concurrent;
using IcsApi.Models;

namespace IcsApi.Services;

/// <summary>
/// In-memory implementation of <see cref="IAuditStore"/>.
/// Stores audit events in a queue with a maximum capacity. Data is not persisted and is lost on application restart.
/// </summary>
public sealed class InMemoryAuditStore : IAuditStore
{
    private const int MaxEvents = 500;
    private readonly ConcurrentQueue<AuditEvent> _events = new();

    public void Add(AuditEvent evt)
    {
        _events.Enqueue(evt);
        while (_events.Count > MaxEvents && _events.TryDequeue(out _))
        {
        }
    }

    public IReadOnlyList<AuditEvent> GetAll() => _events.ToArray().OrderByDescending(e => e.TimestampUtc).ToList();
}
