using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Users;

/// <summary>GuildEditorDataMessageComposer</summary>
[Message(ServerMessages.GuildEditorDataMessageComposer)]
public sealed partial record GuildEditorDataMessageComposer(IReadOnlyList<BadgePartData> BaseParts, IReadOnlyList<BadgePartData> LayerParts, IReadOnlyList<GuildColorData> BadgeColors, IReadOnlyList<GuildColorData> PrimaryColors, IReadOnlyList<GuildColorData> SecondaryColors);
