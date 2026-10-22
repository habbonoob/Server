using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Preferences;

/// <summary>2775 SetUIFlags.</summary>
[Incoming(ClientMessages.SetUIFlagsMessageEvent)]
public sealed partial record SetUIFlagsMessageEvent(int Flags);
