using Habbo.Protocol.Wire;
using Habbo.Silo.Abstractions.Navigator;

namespace Habbo.Protocol.Messages.Outgoing.Navigator;

/// <summary>3064 PopularRoomTagsResultComposer — the tag cloud, each with how many rooms carry it.</summary>
[Message(ServerMessages.PopularRoomTagsResultComposer)]
public sealed partial record PopularRoomTagsResultComposer(IReadOnlyList<RoomTagData> Tags)
{
    public static PopularRoomTagsResultComposer From(IReadOnlyList<RoomTagCount> tags) =>
        new(tags.Select(tag => new RoomTagData(tag.Tag, tag.RoomCount)).ToList());
}
