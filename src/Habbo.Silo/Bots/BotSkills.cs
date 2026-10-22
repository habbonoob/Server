namespace Habbo.Silo.Bots;

/// <summary>The skills a bot's menu offers, by the client's ids (BotSkillEnum).</summary>
public static class BotSkills
{
    /// <summary>Dresses the bot in its owner's look.</summary>
    public const int Figure = 1;

    /// <summary>What it says, and whether and how often it says it by itself.</summary>
    public const int Chatter = 2;

    public const int RandomWalk = 3;

    public const int Dance = 4;

    public const int ChangeName = 5;

    /// <summary>Hands a drink to whoever asks for it by name.</summary>
    public const int ServeBeverage = 6;

    /// <summary>A bot that no one may pick up.</summary>
    public const int NoPickUp = 12;
}
