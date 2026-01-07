namespace IcsApi.Models;

public record AuditEvent(
    DateTime TimestampUtc,
    string? UserObjectId,
    string? Username,
    IReadOnlyList<string> Roles,
    string Method,
    string Path,
    int StatusCode,
    string CorrelationId
);
