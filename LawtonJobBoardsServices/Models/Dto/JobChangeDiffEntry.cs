namespace LawtonJobBoardsServices.Models.Dto;

public record PropertyDiff(string Property, string? OldValue, string? NewValue);

public record JobChangeDiffEntry(
    DateTimeOffset Timestamp,
    int JobId,
    string? JobName,
    IReadOnlyList<PropertyDiff> Diffs
);
