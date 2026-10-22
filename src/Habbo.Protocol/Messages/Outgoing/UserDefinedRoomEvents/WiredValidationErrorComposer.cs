using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents;

/// <summary>3201 WiredValidationErrorComposer — the settings were refused. The client shows the
/// text under <see cref="LocalizationKey"/> with the parameters filled in, and keeps the dialog open.</summary>
[Message(ServerMessages.WiredValidationErrorComposer)]
public sealed partial record WiredValidationErrorComposer(
    string LocalizationKey,
    IReadOnlyList<WiredValidationParameterData> Parameters);
