namespace Habbo.Silo.Items;

/// <summary>What a mystery trophy box holds (config section "MysteryTrophy"): one of these, each as likely.</summary>
public sealed class MysteryTrophyOptions
{
    public List<string> Trophies { get; set; } = [];
}
