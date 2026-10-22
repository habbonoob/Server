namespace Habbo.Domain.Polls;

/// <summary>A poll the hotel runs: what it is called and says, where it is offered, and its questions.</summary>
public sealed class Poll
{
    public int Id { get; set; }

    /// <summary>The kind of poll, as the client is told it.</summary>
    public string Type { get; set; } = string.Empty;

    public string Headline { get; set; } = string.Empty;

    public string Summary { get; set; } = string.Empty;

    public string StartMessage { get; set; } = string.Empty;

    public string EndMessage { get; set; } = string.Empty;

    /// <summary>The room it is offered in; null for any.</summary>
    public int? RoomId { get; set; }

    /// <summary>A net promoter score poll, which the client draws its own way.</summary>
    public bool NpsPoll { get; set; }

    public bool Active { get; set; } = true;

    public List<PollQuestion> Questions { get; set; } = [];
}
