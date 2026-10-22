using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Room.Furniture;

/// <summary>RoomDimmerSavePresetMessageEvent — a dimmer preset; the client sends a fixed false before the item</summary>
[Incoming(ClientMessages.RoomDimmerSavePresetMessageEvent)]
public sealed partial record RoomDimmerSavePresetMessageEvent(int PresetNumber, int EffectType, string ColorHex, int Brightness, bool Apply, bool Reserved, int ItemId);
