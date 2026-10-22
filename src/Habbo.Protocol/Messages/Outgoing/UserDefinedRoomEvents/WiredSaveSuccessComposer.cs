using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents;

/// <summary>3249 WiredSaveSuccessComposer — the settings were saved; the client closes the dialog. It has no body.</summary>
[Message(ServerMessages.WiredSaveSuccessComposer)]
public sealed partial record WiredSaveSuccessComposer;
