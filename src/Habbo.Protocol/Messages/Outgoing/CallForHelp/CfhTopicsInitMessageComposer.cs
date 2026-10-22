using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.CallForHelp;

/// <summary>3272 CfhTopicsInitMessageComposer — the reasons a player may report something for,
/// grouped as the help tool lists them. Sent once at login; the client has no other source.</summary>
[Message(ServerMessages.CfhTopicsInitMessageComposer)]
public sealed partial record CfhTopicsInitMessageComposer(IReadOnlyList<CallForHelpCategoryData> Categories);
