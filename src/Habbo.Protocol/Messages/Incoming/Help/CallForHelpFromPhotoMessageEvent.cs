using System.Globalization;
using Habbo.Protocol.IO;

namespace Habbo.Protocol.Messages.Incoming.Help;

/// <summary>CallForHelpFromPhotoMessageEvent — report a photo. The photo's id is a string, but a numeric one can reach
/// the client's composer as a number, which its encoder writes as an int: read as a string when the rest of the
/// message then fits, as an int otherwise.</summary>
public sealed record CallForHelpFromPhotoMessageEvent(
    string PhotoId, int RoomId, int ReportedUserId, int TopicId, int RoomObjectId, string ReporterName, string ReporterEmail)
    : IIncomingMessage<CallForHelpFromPhotoMessageEvent>
{
    public static ushort Header => ClientMessages.CallForHelpFromPhotoMessageEvent;

    public static CallForHelpFromPhotoMessageEvent Read(MessageReader r)
    {
        var payload = r.ReadBytes(r.Remaining).ToArray();
        return Read(payload, idAsString: true) ?? Read(payload, idAsString: false)
            ?? throw new InvalidDataException($"CallForHelpFromPhoto does not read from {payload.Length} bytes");
    }

    private static CallForHelpFromPhotoMessageEvent? Read(byte[] payload, bool idAsString)
    {
        try
        {
            var r = new MessageReader(payload);
            var message = new CallForHelpFromPhotoMessageEvent(
                idAsString ? r.ReadString() : r.ReadInt().ToString(CultureInfo.InvariantCulture),
                r.ReadInt(), r.ReadInt(), r.ReadInt(), r.ReadInt(), r.ReadString(), r.ReadString());
            return r.Remaining == 0 ? message : null;
        }
        catch (ArgumentOutOfRangeException)
        {
            return null;
        }
    }
}
