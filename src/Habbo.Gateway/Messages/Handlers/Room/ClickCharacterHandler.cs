using Habbo.Protocol.Messages.Incoming.Room;

namespace Habbo.Gateway.Messages.Handlers.Room;

/// <summary>ClickCharacterEvent — the client tells of every click on an avatar, and nothing comes of it: the clicker
/// turns on the LookTo sent with it, or on WiredClickUser in a room whose wired listens for clicks (habbo.com, October
/// 2026: with the click trigger set to keep the direction, ClickCharacter turned nobody).</summary>
internal sealed class ClickCharacterHandler : IMessageHandler<ClickCharacterEvent>
{
    public Task<bool> HandleAsync(GatewaySessionContext context, ClickCharacterEvent message, CancellationToken ct) =>
        Task.FromResult(true);
}
