using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Tracking;

/// <summary>PerformanceLogMessageEvent — how the client performs</summary>
[Incoming(ClientMessages.PerformanceLogMessageEvent)]
public sealed partial record PerformanceLogMessageEvent(int ElapsedSeconds, string UserAgent, string FlashVersion, string OperatingSystem, string Browser, bool IsDebugger, int MemoryKb, int GarbageCollections, int SlowUpdates, int AverageUpdateInterval, int LagSpikes);
