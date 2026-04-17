namespace DndApp.Api.Data;

public sealed class SourceBookEntity
{
    public string Id { get; set; } = string.Empty;
    public string SourceCode { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string VersionTag { get; set; } = string.Empty;
}

public sealed class SourceChapterEntity
{
    public string Id { get; set; } = string.Empty;
    public string SourceBookId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int ChapterOrder { get; set; }
}

public sealed class SourceSectionEntity
{
    public string Id { get; set; } = string.Empty;
    public string SourceChapterId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int SectionOrder { get; set; }
    public int StartLine { get; set; }
    public int EndLine { get; set; }
}

public sealed class SourceBlockEntity
{
    public string Id { get; set; } = string.Empty;
    public string SourceSectionId { get; set; } = string.Empty;
    public string BlockType { get; set; } = string.Empty;
    public string RawText { get; set; } = string.Empty;
    public decimal ParseConfidence { get; set; }
}
