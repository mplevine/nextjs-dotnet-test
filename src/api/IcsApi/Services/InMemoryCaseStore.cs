using System.Collections.Concurrent;
using IcsApi.Models;

namespace IcsApi.Services;

public sealed class InMemoryCaseStore
{
    private readonly ConcurrentDictionary<string, CaseItem> _cases = new();

    public InMemoryCaseStore()
    {
        var now = DateTime.UtcNow;
        Upsert(new CaseItem("CASE-1001", "Initial intake", "Open", now.AddDays(-2)));
        Upsert(new CaseItem("CASE-1002", "Follow-up review", "InReview", now.AddDays(-1)));
        Upsert(new CaseItem("CASE-1003", "Closed example", "Closed", now.AddHours(-12)));
    }

    public IReadOnlyList<CaseItem> GetAll() => _cases.Values.OrderByDescending(c => c.CreatedUtc).ToList();

    public CaseItem? Get(string id) => _cases.TryGetValue(id, out var item) ? item : null;

    public CaseItem Upsert(CaseItem item)
    {
        _cases[item.Id] = item;
        return item;
    }

    public bool Delete(string id) => _cases.TryRemove(id, out _);
}
