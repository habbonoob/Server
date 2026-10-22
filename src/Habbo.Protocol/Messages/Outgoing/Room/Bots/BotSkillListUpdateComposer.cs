using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Room.Bots;

/// <summary>BotSkillListUpdateComposer — the skills a bot's menu offers</summary>
[Message(ServerMessages.BotSkillListUpdateComposer)]
public sealed partial record BotSkillListUpdateComposer(int BotId, IReadOnlyList<BotSkillData> Skills);
