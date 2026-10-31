using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Quest;

/// <summary>GetSeasonalQuestsMessageEvent — the seasonal campaigns running, each with the quest the player is on in it.</summary>
[Incoming(ClientMessages.GetSeasonalQuestsMessageEvent)]
public sealed partial record GetSeasonalQuestsMessageEvent;
