using Habbo.Domain.Users;
using Habbo.Silo.Users;

namespace Habbo.Silo.Items.Interactions;

/// <summary>A changing gate: walking through it puts on the outfit it keeps for one's gender, for as
/// long as one stays in the room; walking through again takes it off. Its state is the two outfits,
/// "boy,girl", which those with rights set.</summary>
[FurniInteraction("furniture_clothing_change")]
public sealed class ClothingChangeInteraction : FurniInteraction
{
    public const char OutfitSeparator = ',';

    public override Task WalkedOnAsync(FurniWalk walk)
    {
        if (walk.User.RoomOutfit is not null)
        {
            walk.Room.DressInRoom(walk.User, null);
            return Task.CompletedTask;
        }

        var outfit = Outfit(walk.Item.Data.StateText, walk.User.Player.Gender);
        if (outfit.Length > 0)
            walk.Room.DressInRoom(walk.User, FigureOutfit.Wear(walk.User.Player.Figure, outfit));
        return Task.CompletedTask;
    }

    /// <summary>The gate's outfit for a gender.</summary>
    public static string Outfit(string state, Gender gender)
    {
        var outfits = state.Split(OutfitSeparator);
        var index = gender == Gender.Female ? 1 : 0;
        return outfits.Length > index ? outfits[index] : string.Empty;
    }

    /// <summary>The gate's state with one gender's outfit replaced.</summary>
    public static string WithOutfit(string state, Gender gender, string outfit)
    {
        var boy = gender == Gender.Male ? outfit : Outfit(state, Gender.Male);
        var girl = gender == Gender.Female ? outfit : Outfit(state, Gender.Female);
        return boy + OutfitSeparator + girl;
    }
}
