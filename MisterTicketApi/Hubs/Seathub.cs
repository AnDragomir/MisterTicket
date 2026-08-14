using Microsoft.AspNetCore.SignalR;

namespace MisterTicketApi.Hubs;

/// <summary>
/// Real-time seat updates. Clients join the group of the event they are looking
/// at, and the server pushes "SeatsChanged" to that group whenever seats move.
///
/// No authentication: seat statuses are public, the map is visible to anyone.
/// </summary>
public class SeatHub : Hub
{
    /// <summary>Group name for one event, used by the hub and by the services.</summary>
    public static string GroupFor(int eventId) => $"event-{eventId}";

    /// <summary>Called by the Angular page when it opens a seat map.</summary>
    public async Task JoinEvent(int eventId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, GroupFor(eventId));
    }

    /// <summary>Called when the page is left. Disconnects clean up on their own.</summary>
    public async Task LeaveEvent(int eventId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupFor(eventId));
    }
}