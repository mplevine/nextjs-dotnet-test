namespace IcsApi.Models;

public record CaseItem(
    string Id,
    string Title,
    string Status,
    DateTime CreatedUtc
);
