using Habbo.Domain.Rooms;
using Habbo.Silo.Abstractions.Chat;

namespace Habbo.Protocol.Messages.Outgoing.Room.Chat;

/// <summary>Shared projection for the three chat messages, which carry identical payloads.</summary>
internal static class ChatLinks
{
    public static List<ChatLinkWireData> From(IReadOnlyList<ChatLink> links) =>
        links.Select(link => new ChatLinkWireData(link.Text, link.Url, link.Trusted)).ToList();
}
