namespace Habbo.Domain.Navigator;

/// <summary>A room lifted to the new navigator's top strip, with its own picture and caption.</summary>
public sealed class LiftedRoom
{
    public int Id { get; set; }

    public int RoomId { get; set; }

    /// <summary>Which strip area it is shown in.</summary>
    public int AreaId { get; set; }

    public string Image { get; set; } = string.Empty;

    public string Caption { get; set; } = string.Empty;

    public int SortOrder { get; set; }
}
