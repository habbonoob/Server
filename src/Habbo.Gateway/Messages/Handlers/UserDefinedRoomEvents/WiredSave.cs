using Habbo.Silo.Abstractions.Wired;

namespace Habbo.Gateway.Messages.Handlers.UserDefinedRoomEvents;

/// <summary>Hands a saved wired dialog to the player's room, which answers the player itself.</summary>
internal static class WiredSave
{
    public static async Task<bool> SendAsync(GatewaySessionContext context, WiredSaveData save)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.SaveWiredAsync(player.Id, save);
        return true;
    }
}
