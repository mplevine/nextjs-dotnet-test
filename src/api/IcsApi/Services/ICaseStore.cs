using IcsApi.Models;

namespace IcsApi.Services;

/// <summary>
/// Interface for case storage operations.
/// Defines the contract for case data persistence.
/// </summary>
public interface ICaseStore
{
    /// <summary>
    /// Gets all cases, ordered by creation date (newest first).
    /// </summary>
    IReadOnlyList<CaseItem> GetAll();

    /// <summary>
    /// Gets a specific case by ID.
    /// </summary>
    /// <param name="id">The case ID</param>
    /// <returns>The case item, or null if not found</returns>
    CaseItem? Get(string id);

    /// <summary>
    /// Creates or updates a case.
    /// </summary>
    /// <param name="item">The case item to save</param>
    /// <returns>The saved case item</returns>
    CaseItem Upsert(CaseItem item);

    /// <summary>
    /// Deletes a case by ID.
    /// </summary>
    /// <param name="id">The case ID</param>
    /// <returns>True if the case was deleted, false if not found</returns>
    bool Delete(string id);
}
