using Microsoft.AspNetCore.SignalR;

namespace EnmyAuto.Api.Hubs;

/// <summary>
/// Clients call <see cref="SubscribeToStoryboard"/> on connect so they only receive
/// events for their own storyboard render job.
/// </summary>
public sealed class RenderHub : Hub
{
    public const string Endpoint = "/hubs/render";

    /// <summary>Group name for a given storyboard — used by server-side push.</summary>
    public static string GroupName(Guid storyboardId) => $"render:{storyboardId}";

    /// <summary>Client-callable: join the SignalR group for the given storyboard.</summary>
    public async Task SubscribeToStoryboard(Guid storyboardId) =>
        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(storyboardId));

    /// <summary>Client-callable: leave the group (e.g., when navigating away).</summary>
    public async Task UnsubscribeFromStoryboard(Guid storyboardId) =>
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(storyboardId));
}
