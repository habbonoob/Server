using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Competition;

/// <summary>SecondsUntilMessageComposer — the seconds until the time asked about</summary>
[Message(ServerMessages.SecondsUntilMessageComposer)]
public sealed partial record SecondsUntilMessageComposer(string TimeStr, int SecondsUntil);
