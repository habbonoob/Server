using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents;

/// <summary>A value put into a validation error's text, by its placeholder name.</summary>
[WireStruct]
public sealed partial record WiredValidationParameterData(string Key, string Value);
