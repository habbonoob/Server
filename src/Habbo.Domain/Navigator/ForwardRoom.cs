namespace Habbo.Domain.Navigator;

/// <summary>A named room a link can send users to ("navigator/goto/predefined_noob_lobby").</summary>
public sealed class ForwardRoom
{
    public required string Type { get; set; }

    public int RoomId { get; set; }
}
