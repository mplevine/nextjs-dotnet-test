using IcsApi.Models;

namespace IcsApi.Services;

/// <summary>
/// Interface for audit event storage operations.
/// Defines the contract for audit trail persistence.
/// </summary>
public interface IAuditStore
{
    /// <summary>
    /// Adds an audit event to the store.
    /// </summary>
    /// <param name="evt">The audit event to record</param>
    void Add(AuditEvent evt);

    /// <summary>
    /// Gets all audit events, ordered by timestamp (newest first).
    /// </summary>
    /// <returns>Read-only list of audit events</returns>
    IReadOnlyList<AuditEvent> GetAll();
}
