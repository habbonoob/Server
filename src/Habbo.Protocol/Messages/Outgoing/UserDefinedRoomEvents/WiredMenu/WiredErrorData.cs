using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.WiredMenu;

/// <summary>A kind of wired error and how often it happened: its id (wiredmenu.error_info.N), name,
/// category (which picks its icon), count, and how long ago it last did.</summary>
[WireStruct]
public sealed partial record WiredErrorData(int ErrorId, string ErrorName, string Category, int ThrowCount, long MillisecondsSinceLast);
