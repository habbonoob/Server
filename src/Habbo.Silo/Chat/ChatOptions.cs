namespace Habbo.Silo.Chat;

/// <summary>The hotel's chat rules (config section "Chat").</summary>
public sealed class ChatOptions
{
    /// <summary>What a filtered word becomes, as Habbo's filter writes it.</summary>
    public string FilterReplacement { get; set; } = "bobba";

    /// <summary>The longest line said in a room, as the client's chat input allows; more is cut off.</summary>
    public int MaxMessageLength { get; set; } = 100;

    /// <summary>Flood control by the room's sensitivity (0 low, 1 normal, 2 high): how many lines a
    /// user may say within the window before the room mutes them for the penalty.</summary>
    public List<FloodRule> FloodRules { get; set; } = [];
}
