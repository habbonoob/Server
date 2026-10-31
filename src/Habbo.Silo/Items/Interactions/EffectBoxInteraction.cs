using System.Globalization;
using Habbo.Protocol.Messages.Outgoing.Notifications;
using Habbo.Silo.Abstractions.Furni;

namespace Habbo.Silo.Items.Interactions;

/// <summary>An FX box: its owner opens it for the effect its customparams name (fxbox_fx92: 92), theirs for good and on at
/// once, and the box is gone. One whose effect they already have for good stays shut. habbo.com, 31 October 2026, opening
/// a Ghost box: UnseenItems (effects: 92), ObjectRemove (picked up by -1, after 792 ms), AvatarEffectAdded and
/// AvatarEffectActivated (92, 86400 s, permanent), the tile's height and the avatar wearing 92; a second Ghost box only
/// "permanent_effect_exists", then the same UnseenItems.</summary>
[FurniInteraction("furniture_effectbox")]
public sealed class EffectBoxInteraction : FurniInteraction
{
    /// <summary>The notification for a box whose effect the player already has for good.</summary>
    public const string PermanentEffectExists = "permanent_effect_exists";

    /// <summary>How long the client keeps an opened box in view, and whom its removal names.</summary>
    private const int RemovalDelayMilliseconds = 792;
    private const int NoPicker = -1;

    public override FurniUsagePolicy UsagePolicy => FurniUsagePolicy.Everyone;

    public override async Task UseAsync(FurniUse use)
    {
        if (use.Action != FurniAction.Use || use.Item.Item.OwnerId != use.User.Player.Id || use.Item.Temporary)
            return;
        if (!int.TryParse(use.Item.Item.Definition!.CustomParams, NumberStyles.Integer, CultureInfo.InvariantCulture, out var effectId))
            return;

        if (await use.Room.HasPermanentEffectAsync(use.User, effectId))
        {
            use.Room.Tell(use.User, new NotificationDialogMessageComposer(PermanentEffectExists, []));
            use.Room.Tell(use.User, UnseenItemsComposer.Effects([effectId]));
            return;
        }

        use.Room.Tell(use.User, UnseenItemsComposer.Effects([effectId]));
        // opened elsewhere meanwhile: nothing to give
        if (!await use.Room.RemoveAsync(use.Item, RemovalDelayMilliseconds, NoPicker))
            return;

        await use.Room.GrantPermanentEffectAsync(use.User, effectId);
        use.Room.WearEffect(use.User, effectId);
    }
}
