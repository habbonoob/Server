using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Room.Bots;

/// <summary>BotCommandConfigurationComposer — how one of a bot's skills is set up</summary>
[Message(ServerMessages.BotCommandConfigurationComposer)]
public sealed partial record BotCommandConfigurationComposer(int BotId, int SkillId, string Data);
