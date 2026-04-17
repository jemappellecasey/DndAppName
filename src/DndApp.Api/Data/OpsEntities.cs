namespace DndApp.Api.Data;

public sealed class IngestionRunEntity
{
    public string Id { get; set; } = string.Empty;
    public string SourceCode { get; set; } = string.Empty;
    public string VersionTag { get; set; } = string.Empty;
    public DateTimeOffset StartedAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Checksum { get; set; } = string.Empty;
}

public sealed class ReviewQueueEntity
{
    public string Id { get; set; } = string.Empty;
    public string IngestionRunId { get; set; } = string.Empty;
    public string QueueType { get; set; } = string.Empty;
    public string ReferenceId { get; set; } = string.Empty;
    public decimal Confidence { get; set; }
    public string Notes { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public sealed class CorrectionOverrideEntity
{
    public string Id { get; set; } = string.Empty;
    public string ReviewQueueId { get; set; } = string.Empty;
    public string OverrideJson { get; set; } = "{}";
    public string AppliedBy { get; set; } = string.Empty;
    public DateTimeOffset AppliedAtUtc { get; set; }
}

public sealed class ImportReportEntity
{
    public string Id { get; set; } = string.Empty;
    public string IngestionRunId { get; set; } = string.Empty;
    public string ReportJson { get; set; } = "{}";
    public DateTimeOffset CreatedAtUtc { get; set; }
}
