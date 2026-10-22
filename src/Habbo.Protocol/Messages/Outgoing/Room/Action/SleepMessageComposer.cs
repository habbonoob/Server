using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Room.Action;

/// <summary>3252 SleepMessageComposer — an avatar falls asleep (the Zzz), or wakes up.</summary>
[Message(ServerMessages.SleepMessageComposer)]
public sealed partial record SleepMessageComposer(int RoomIndex, bool Asleep);
