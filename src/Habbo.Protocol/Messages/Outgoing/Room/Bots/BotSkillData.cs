using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Room.Bots;

/// <summary>BotSkillData — a skill a bot's menu offers, with what it needs</summary>
[WireStruct]
public sealed partial record BotSkillData(int Id, string Data);
