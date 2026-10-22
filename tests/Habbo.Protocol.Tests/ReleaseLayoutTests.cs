using System.Buffers;
using Habbo.Domain.Users;
using Habbo.Protocol.Composers;
using Habbo.Protocol.IO;
using Habbo.Protocol.Messages.Incoming.Preferences;
using Habbo.Protocol.Messages.Incoming.Marketplace;
using Habbo.Protocol.Messages.Incoming.RoomSettings;
using Habbo.Protocol.Messages.Outgoing.Inventory.Badges;
using Habbo.Protocol.Messages.Outgoing.Marketplace;
using Habbo.Protocol.Messages.Outgoing.Room.Engine;
using Habbo.Protocol.Messages.Outgoing.Preferences;
using Habbo.Protocol.Messages.Outgoing.Room.Chat;
using Habbo.Protocol.Wire;
using Habbo.Silo.Abstractions.Furni;
using Habbo.Silo.Abstractions.Players;

namespace Habbo.Protocol.Tests;

/// <summary>Messages each client release lays out its own way ([WireSince], [WireUntil]).</summary>
public class ReleaseLayoutTests
{
    private static AccountPreferences Preferences => new(
        UiVolume: 10, FurniVolume: 20, TraxVolume: 30, FreeFlowChatDisabled: false, RoomInvitesIgnored: true,
        RoomCameraFollowDisabled: false, UiFlags: 4, PreferedChatStyle: 7, WiredMenuButton: true, WiredInspectButton: false,
        PlayTestMode: false, WiredWhisperDisabled: true, ShowAllNotifications: true, WiredUiStyle: "dark", InfoFeedEnabled: true,
        ChatFontSize: 3, ChatMode: 1, ChatBubbleWidth: 2, ChatScrollSpeed: 0, OnlineIndicator: OnlineIndicatorPreference.Nobody);

    private static byte[] Payload(IComposer composer, string release)
    {
        var buffer = new ArrayBufferWriter<byte>();
        composer.Compose(new MessageWriter(buffer, release));
        return buffer.WrittenSpan.ToArray();
    }

    [Fact]
    public void AccountPreferencesEndWithTheChatSettingsForSeptember2026Only()
    {
        var composer = AccountPreferencesComposer.From(Preferences);
        var march = Payload(composer, ClientReleases.March2026);
        var september = Payload(composer, ClientReleases.September2026);

        Assert.Equal(march, september[..march.Length]);
        var rest = new MessageReader(september.AsMemory(march.Length));
        Assert.Equal(3, rest.ReadInt());
        Assert.Equal(1, rest.ReadInt());
        Assert.Equal(2, rest.ReadInt());
        Assert.Equal(0, rest.ReadInt());
        Assert.Equal((int)OnlineIndicatorPreference.Nobody, rest.ReadInt());
        Assert.Equal(0, rest.Remaining);
    }

    [Fact]
    public void ARoomsChatSettingsAreOnlyItsFloodSensitivityForSeptember2026()
    {
        var composer = new RoomChatSettingsMessageComposer(new RoomChatSettingsData(1, 2, 0, 14, 2));

        var march = new MessageReader(Payload(composer, ClientReleases.March2026));
        Assert.Equal([1, 2, 0, 14, 2], Enumerable.Range(0, 5).Select(_ => march.ReadInt()));
        Assert.Equal(0, march.Remaining);

        var september = new MessageReader(Payload(composer, ClientReleases.September2026));
        Assert.Equal(2, september.ReadInt());
        Assert.Equal(0, september.Remaining);
    }

    [Fact]
    public void AVaryingMessageCarriesEachReleasesPayload()
    {
        var message = new RoomChatSettingsMessageComposer(new RoomChatSettingsData(1, 2, 0, 14, 2)).ToMessage();

        Assert.Equal(20, message.Payload.Length);
        Assert.Equal(4, message.PayloadFor(ClientReleases.September2026).Length);
    }

    [Fact]
    public void ChatPreferencesAreReadAsTheClientsReleaseSendsThem()
    {
        // [false, mode, bubble width, scroll speed]
        byte[] payload = [0, 0, 0, 0, 1, 0, 0, 0, 2, 0, 0, 0, 0];

        var september = SetChatPreferencesMessageEvent.Read(new MessageReader(payload) { Release = ClientReleases.September2026 });
        Assert.Equal(new SetChatPreferencesMessageEvent(false, 1, 2, 0), september);

        var march = SetChatPreferencesMessageEvent.Read(new MessageReader(payload[..1]));
        Assert.Equal(new SetChatPreferencesMessageEvent(false, null, null, null), march);
    }

    [Fact]
    public void September2026SavesHowTheRoomTreatsVisitorsInsteadOfItsChatLayout()
    {
        var buffer = new ArrayBufferWriter<byte>();
        var w = new MessageWriter(buffer);
        w.WriteInt(7); w.WriteString("Lounge"); w.WriteString(""); w.WriteInt(0); w.WriteString(""); w.WriteInt(25); w.WriteInt(1);
        w.WriteInt(0); // no tags
        w.WriteInt(0); w.WriteBoolean(true); w.WriteBoolean(false); w.WriteBoolean(true); w.WriteBoolean(false); w.WriteInt(0); w.WriteInt(0);
        w.WriteInt(1); w.WriteInt(1); w.WriteInt(1);
        w.WriteInt(2); // flood sensitivity
        w.WriteBoolean(false); w.WriteBoolean(true); w.WriteInt(120); w.WriteBoolean(true); w.WriteInt(600); w.WriteBoolean(true);

        var saved = SaveRoomSettingsMessageEvent.Read(new MessageReader(buffer.WrittenMemory) { Release = ClientReleases.September2026 });

        Assert.Null(saved.ChatMode);
        Assert.Null(saved.ChatFullHearRange);
        Assert.Null(saved.AllowNavigatorDynamicCategories);
        Assert.Equal(2, saved.ChatFloodSensitivity);
        Assert.Equal((false, true, 120, true, 600, true),
            (saved.LeaveOnDoorTile, saved.IdleSleepEnabled, saved.IdleSleepTimeoutSeconds, saved.IdleAutokickEnabled,
                saved.IdleAutokickTimeoutSeconds, saved.MuteAllPets));
    }

    [Fact]
    public void ABadgeCarriesItsOwnersAndRarityForSeptember2026()
    {
        var composer = new BadgesComposer(1, 0, [new BadgeData(7, "ADM", 1, BadgeRarity.Unique)]);

        var march = new MessageReader(Payload(composer, ClientReleases.March2026));
        Assert.Equal((1, 0, 1, 7, "ADM"), (march.ReadInt(), march.ReadInt(), march.ReadInt(), march.ReadInt(), march.ReadString()));
        Assert.Equal(0, march.Remaining);

        var september = new MessageReader(Payload(composer, ClientReleases.September2026));
        Assert.Equal((1, 0, 1, 7, "ADM"), (september.ReadInt(), september.ReadInt(), september.ReadInt(), september.ReadInt(), september.ReadString()));
        Assert.Equal((1, (int)BadgeRarity.Unique), (september.ReadInt(), september.ReadInt()));
        Assert.Equal(0, september.Remaining);
    }

    private static FurniStuffData Crackable(int hits, int target) => FurniStuffData.From(new FurniDataView(
        (int)Habbo.Domain.Furniture.Data.FurniDataType.Crackable, "", null, null, null, null, null, null, null, hits, target, null, null));

    [Fact]
    public void AUsableItemIsAFloorItemToMarch2026AndSaysWhetherItWasUsedToSeptember2026()
    {
        var offer = new MarketplaceOfferData(5, MarketplaceOfferData.Open, MarketplaceOfferData.Legacy(MarketplaceOfferData.UsableItem),
            MarketplaceOfferData.UsableItem, 4987, Crackable(3, 20000), null, null, null, Used: true, 4000, 60, 0, 1);
        var composer = new MarketPlaceOffersComposer([offer], 1);

        var march = new MessageReader(Payload(composer, ClientReleases.March2026));
        Assert.Equal((1, 5, MarketplaceOfferData.Open, MarketplaceOfferData.FloorItem, 4987), (march.ReadInt(), march.ReadInt(), march.ReadInt(), march.ReadInt(), march.ReadInt()));
        var september = new MessageReader(Payload(composer, ClientReleases.September2026));
        Assert.Equal((1, 5, MarketplaceOfferData.Open, MarketplaceOfferData.UsableItem, 4987), (september.ReadInt(), september.ReadInt(), september.ReadInt(), september.ReadInt(), september.ReadInt()));

        // the same stuff data follows for both; September 2026 then reads the used flag
        Assert.Equal(march.Remaining + 1, september.Remaining);
    }

    [Fact]
    public void ASoldOwnOfferSaysWhenItSoldToSeptember2026()
    {
        var offer = new MarketplaceOwnOfferData(5, MarketplaceOfferData.Sold, MarketplaceOfferData.WallItem, MarketplaceOfferData.WallItem,
            4001, null, "2005", null, null, null, 65000, 0, 65000, StatusTime: 1_790_000_000_000);
        var composer = new MarketPlaceOwnOffersComposer(100, [offer]);

        var march = Payload(composer, ClientReleases.March2026);
        var september = Payload(composer, ClientReleases.September2026);
        Assert.Equal(march.Length + 8, september.Length);
        var time = new MessageReader(september.AsMemory(september.Length - 8));
        Assert.Equal(1_790_000_000_000, time.ReadLong());
    }

    [Fact]
    public void MarchSellsOneItemAndSeptemberSeveral()
    {
        byte[] march = [0, 0, 0, 50, 0, 0, 0, 1, 0, 0, 0, 9];
        Assert.Equal(new MakeOfferMessageEvent(50, 1, 9, null), MakeOfferMessageEvent.Read(new MessageReader(march)));

        byte[] september = [0, 0, 0, 50, 0, 0, 0, 1, 0, 0, 0, 2, 0, 0, 0, 9, 0, 0, 0, 10];
        var several = MakeOfferMessageEvent.Read(new MessageReader(september) { Release = ClientReleases.September2026 });
        Assert.Null(several.ItemId);
        Assert.Equal([9, 10], several.ItemIds!);
    }
}
