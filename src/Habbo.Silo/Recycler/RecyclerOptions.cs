namespace Habbo.Silo.Recycler;

/// <summary>The Furni-Matic (config section "Recycler").</summary>
public sealed class RecyclerOptions
{
    public bool Enabled { get; set; }

    /// <summary>How many items go in at once.</summary>
    public int ItemsRequired { get; set; } = 5;

    /// <summary>How long a player waits between uses.</summary>
    public int TimeoutSeconds { get; set; }

    /// <summary>The box a prize comes in.</summary>
    public string BoxClassName { get; set; } = string.Empty;

    /// <summary>How the box's card writes the day it was received.</summary>
    public string BoxDateFormat { get; set; } = "dd-MM-yyyy";
}
