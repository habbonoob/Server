using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Handshake;

/// <summary>1861 VersionCheck — the client's build number and external variables url.</summary>
[Incoming(ClientMessages.VersionCheckMessageEvent)]
public sealed partial record VersionCheckMessageEvent(int BuildNumber, string ExternalVariablesUrl);
