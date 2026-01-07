using System.Collections.Concurrent;
using IcsApi.Models;

namespace IcsApi.Services;

public sealed class InMemoryAuditStore
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
