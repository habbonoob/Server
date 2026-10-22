namespace Habbo.Silo.Abstractions.Groups;

/// <summary>How far into a forum the player has read.</summary>
[GenerateSerializer]
public sealed partial record ForumReadMark(
    [property: Id(0)] int GroupId, [property: Id(1)] int LastReadMessageIndex, [property: Id(2)] bool Reset);
