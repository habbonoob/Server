namespace Habbo.Silo.Wired.Variables;

/// <summary>Where a variable change came from, as the variable trigger's "Allow triggering from" bits number them
/// (wiredfurni.params.variables.trigger_origin).</summary>
public static class WiredVariableOrigins
{
    /// <summary>Wired in this room.</summary>
    public const int ThisRoom = 1 << 0;

    /// <summary>Wired in another room, through a shared variable.</summary>
    public const int OtherRoom = 1 << 1;

    /// <summary>Someone in the wired menu's variable inspection.</summary>
    public const int Inspection = 1 << 2;

    /// <summary>From outside the hotel, through the web API.</summary>
    public const int External = 1 << 3;

    /// <summary>What the client sends when every option it enables is ticked.</summary>
    public const int Every = -1;
}
