namespace EnmyAuto.Api.Models.Hubs;

public sealed record RenderProgressEvent(
    Guid StoryboardId,
    RenderEventType EventType,
    int ProgressPercent,
    string? OutputPath,
    string? ErrorMessage,
    DateTime Timestamp);

public enum RenderEventType
{
    Started,
    Progress,
    Completed,
    Failed
}
