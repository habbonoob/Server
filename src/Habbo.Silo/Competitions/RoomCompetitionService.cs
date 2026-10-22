using System.Globalization;
using Habbo.Domain.Competitions;
using Habbo.Domain.Rooms;
using Habbo.Infrastructure;
using Habbo.Protocol.Messages.Outgoing.Competition;
using Habbo.Protocol.Messages.Outgoing.Room.Session;
using Habbo.Silo.Abstractions.Navigator;
using Habbo.Silo.Abstractions.Players;
using Habbo.Silo.Achievements;
using Habbo.Silo.Hotel;
using Habbo.Silo.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Habbo.Silo.Competitions;

/// <summary>Room competitions. While one is on, an owner going into a room of theirs is asked to enter it: they accept
/// the rules, and a room made since the competition began, open to all and with the furni it asks for, goes in once
/// submitted and confirmed; a player enters one room at most. Others going into a room entered may vote for it, with
/// so many votes a day of the hotel's, when they hold the badge it asks for.</summary>
public sealed class RoomCompetitionService(
    IDbContextFactory<AppDbContext> dbFactory,
    IOptionsMonitor<RoomCompetitionOptions> options,
    IOptionsMonitor<HotelAvailabilityOptions> hotel,
    PlayerMessenger messenger,
    IGrainFactory grains) : IRoomCompetitionService
{
    /// <summary>How the landing view writes the times it counts down to, in the hotel's time.</summary>
    private const string TimeFormat = "yyyy-MM-dd HH:mm";

    /// <summary>What sets a colour variant apart in its class name: "chair_norja*2".</summary>
    private const char ColourSeparator = '*';

    private const int NoRoom = 0;

    public async Task SendRoomInfoAsync(PlayerData player, int roomId)
    {
        if (Current() is not { } competition)
            return;

        await using var db = await dbFactory.CreateDbContextAsync();
        if (await db.Rooms.AsNoTracking().FirstOrDefaultAsync(r => r.Id == roomId) is not { } room)
            return;

        if (room.OwnerId == player.Id)
        {
            // one who entered another room already is not asked again
            var entry = await EntryOfAsync(db, competition, player.Id);
            if (entry is null || entry.RoomId == roomId)
                await SendSubmitResultAsync(db, player, competition, room, await StandingAsync(db, player, competition, room, entry));
            return;
        }

        if (await db.RoomCompetitionEntries.AnyAsync(e => e.GoalId == competition.GoalId && e.RoomId == roomId))
            await SendVotingInfoAsync(db, player, competition);
    }

    public async Task SubmitAsync(PlayerData player, int roomId, string goalCode, int step)
    {
        if (Current() is not { } competition || competition.GoalCode != goalCode)
            return;

        await using var db = await dbFactory.CreateDbContextAsync();
        if (await db.Rooms.AsNoTracking().FirstOrDefaultAsync(r => r.Id == roomId && r.OwnerId == player.Id) is not { } room)
            return;

        var entry = await EntryOfAsync(db, competition, player.Id);
        if (entry is not null && entry.RoomId != roomId)
            return;

        if ((RoomCompetitionSubmitStep)step == RoomCompetitionSubmitStep.AcceptRules
            && !await db.RoomCompetitionRulesAcceptances.AnyAsync(a => a.GoalId == competition.GoalId && a.UserId == player.Id))
        {
            db.RoomCompetitionRulesAcceptances.Add(new RoomCompetitionRulesAcceptance { GoalId = competition.GoalId, UserId = player.Id });
            await db.SaveChangesAsync();
        }

        var standing = await StandingAsync(db, player, competition, room, entry);
        if (standing == RoomCompetitionSubmitResult.CanSubmit)
        {
            switch ((RoomCompetitionSubmitStep)step)
            {
                case RoomCompetitionSubmitStep.Submit:
                    standing = RoomCompetitionSubmitResult.ConfirmSubmit;
                    break;
                case RoomCompetitionSubmitStep.Confirm:
                    db.RoomCompetitionEntries.Add(new RoomCompetitionEntry { GoalId = competition.GoalId, RoomId = roomId, UserId = player.Id });
                    await db.SaveChangesAsync();
                    standing = RoomCompetitionSubmitResult.Entered;
                    break;
            }
        }

        await SendSubmitResultAsync(db, player, competition, room, standing);
    }

    public async Task VoteAsync(PlayerData player, int roomId, string goalCode)
    {
        if (Current() is not { } competition || competition.GoalCode != goalCode)
            return;

        await using var db = await dbFactory.CreateDbContextAsync();
        var entered = await db.RoomCompetitionEntries.AsNoTracking().FirstOrDefaultAsync(e => e.GoalId == competition.GoalId && e.RoomId == roomId);
        if (entered is null || entered.UserId == player.Id)
            return;

        // a vote is for a room, once: the day's votes are to spread over the entries, not to spend on one
        if (await MayVoteAsync(db, player, competition) == RoomCompetitionVoteResult.MayVote
            && await VotesLeftAsync(db, player, competition) > 0
            && !await db.RoomCompetitionVotes.AnyAsync(v => v.GoalId == competition.GoalId && v.RoomId == roomId && v.VoterId == player.Id))
        {
            db.RoomCompetitionVotes.Add(new RoomCompetitionVote { GoalId = competition.GoalId, RoomId = roomId, VoterId = player.Id });
            await db.SaveChangesAsync();
            await grains.GetGrain<IPlayerGrain>(player.Id).ProgressAchievementAsync(AchievementNames.RoomCompetitionVoter, 1);
        }

        await SendVotingInfoAsync(db, player, competition);
    }

    public async Task SendIsPartOfAsync(PlayerData player, string goalCode)
    {
        if (Current() is not { } competition || competition.GoalCode != goalCode)
        {
            await messenger.SendAsync(player.Id, new IsUserPartOfCompetitionMessageComposer(false, NoRoom));
            return;
        }

        await using var db = await dbFactory.CreateDbContextAsync();
        var entry = await EntryOfAsync(db, competition, player.Id);
        await messenger.SendAsync(player.Id, new IsUserPartOfCompetitionMessageComposer(entry is not null, entry?.RoomId ?? NoRoom));
    }

    public async Task ForwardToEntryAsync(PlayerData player, string goalCode, int roomId)
    {
        if (Current() is not { } competition || competition.GoalCode != goalCode)
            return;

        await using var db = await dbFactory.CreateDbContextAsync();
        if (await db.RoomCompetitionEntries.AnyAsync(e => e.GoalId == competition.GoalId && e.RoomId == roomId))
            await messenger.SendAsync(player.Id, new RoomForwardMessageComposer(roomId));
    }

    public async Task ForwardToRandomEntryAsync(PlayerData player, string goalCode)
    {
        if (Current() is not { } competition || competition.GoalCode != goalCode)
            return;

        await using var db = await dbFactory.CreateDbContextAsync();
        var rooms = await db.RoomCompetitionEntries.AsNoTracking().Where(e => e.GoalId == competition.GoalId).Select(e => e.RoomId).ToListAsync();
        if (rooms.Count > 0)
            await messenger.SendAsync(player.Id, new RoomForwardMessageComposer(rooms[Random.Shared.Next(rooms.Count)]));
    }

    public async Task ForwardToSubmittableRoomAsync(PlayerData player)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var competition = Current();
        var rooms = db.Rooms.AsNoTracking().Where(r => r.OwnerId == player.Id);
        if (competition is not null)
            rooms = rooms.Where(r => r.CreatedAt >= competition.StartsAt.ToUniversalTime());
        var newest = await rooms.OrderByDescending(r => r.CreatedAt).Select(r => (int?)r.Id).FirstOrDefaultAsync();

        await messenger.SendAsync(player.Id, newest is { } roomId
            ? new RoomForwardMessageComposer(roomId)
            : new NoOwnedRoomsAlertMessageComposer());
    }

    public async Task SendSecondsUntilAsync(PlayerData player, string time)
    {
        if (!DateTime.TryParseExact(time, TimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var local))
            return;

        // a time the clocks skip when they go forward is taken as the moment after the skip; converting it would throw
        var zone = HotelZone();
        local = DateTime.SpecifyKind(local, DateTimeKind.Unspecified);
        for (var step = 0; step < 4 && zone.IsInvalidTime(local); step++)
            local = local.AddMinutes(30);
        if (zone.IsInvalidTime(local))
            return;

        var at = TimeZoneInfo.ConvertTimeToUtc(local, zone);
        var seconds = Math.Max(0, (int)(at - DateTime.UtcNow).TotalSeconds);
        await messenger.SendAsync(player.Id, new SecondsUntilMessageComposer(time, seconds));
    }

    /// <summary>The competition on now, if any: the first of the hotel's whose dates are now.</summary>
    private RoomCompetition? Current()
    {
        var now = DateTimeOffset.UtcNow;
        return options.CurrentValue.Competitions.FirstOrDefault(c => c.GoalCode.Length > 0 && c.IsOn(now));
    }

    private static Task<RoomCompetitionEntry?> EntryOfAsync(AppDbContext db, RoomCompetition competition, Guid playerId) =>
        db.RoomCompetitionEntries.AsNoTracking().FirstOrDefaultAsync(e => e.GoalId == competition.GoalId && e.UserId == playerId);

    /// <summary>Where entering the room stands, before any step the owner takes.</summary>
    private static async Task<RoomCompetitionSubmitResult> StandingAsync(
        AppDbContext db, PlayerData player, RoomCompetition competition, Room room, RoomCompetitionEntry? entry)
    {
        if (entry is not null)
            return RoomCompetitionSubmitResult.Entered;
        if (competition.RulesToAccept && !await db.RoomCompetitionRulesAcceptances.AnyAsync(a => a.GoalId == competition.GoalId && a.UserId == player.Id))
            return RoomCompetitionSubmitResult.AcceptRules;
        if (room.CreatedAt < competition.StartsAt)
            return RoomCompetitionSubmitResult.RoomTooOld;
        if (room.State != RoomState.Open)
            return RoomCompetitionSubmitResult.DoorNotOpen;
        if ((await MissingFurniAsync(db, competition, room.Id)).Count > 0)
            return RoomCompetitionSubmitResult.MissingFurni;
        return RoomCompetitionSubmitResult.CanSubmit;
    }

    /// <summary>The furni the competition asks for that the room has none of; a colour variant counts as its kind.</summary>
    private static async Task<List<string>> MissingFurniAsync(AppDbContext db, RoomCompetition competition, int roomId)
    {
        if (competition.RequiredFurni.Count == 0)
            return [];

        var placed = (await db.UserItems.AsNoTracking().Where(i => i.RoomId == roomId).Select(i => i.Definition!.ClassName).Distinct().ToListAsync())
            .SelectMany(name => new[] { name, name.Split(ColourSeparator)[0] })
            .ToHashSet(StringComparer.Ordinal);
        return competition.RequiredFurni.Where(name => !placed.Contains(name)).ToList();
    }

    private async Task SendSubmitResultAsync(AppDbContext db, PlayerData player, RoomCompetition competition, Room room, RoomCompetitionSubmitResult result)
    {
        var missing = result == RoomCompetitionSubmitResult.MissingFurni ? await MissingFurniAsync(db, competition, room.Id) : [];
        await messenger.SendAsync(player.Id, new CompetitionEntrySubmitResultMessageComposer(
            competition.GoalId, competition.GoalCode, (int)result, competition.RequiredFurni, missing));
    }

    private async Task SendVotingInfoAsync(AppDbContext db, PlayerData player, RoomCompetition competition) =>
        await messenger.SendAsync(player.Id, new CompetitionVotingInfoMessageComposer(
            competition.GoalId, competition.GoalCode, (int)await MayVoteAsync(db, player, competition), await VotesLeftAsync(db, player, competition)));

    private static async Task<RoomCompetitionVoteResult> MayVoteAsync(AppDbContext db, PlayerData player, RoomCompetition competition) =>
        competition.VoteRequiredBadge.Length == 0 || await db.UserBadges.AnyAsync(b => b.UserId == player.Id && b.Code == competition.VoteRequiredBadge)
            ? RoomCompetitionVoteResult.MayVote
            : RoomCompetitionVoteResult.BadgeMissing;

    /// <summary>The votes the player has left today, the day as the hotel counts it.</summary>
    private async Task<int> VotesLeftAsync(AppDbContext db, PlayerData player, RoomCompetition competition)
    {
        var zone = HotelZone();
        var today = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, zone).Date;
        var startOfDay = new DateTimeOffset(today, zone.GetUtcOffset(today)).ToUniversalTime();
        var cast = await db.RoomCompetitionVotes.CountAsync(v => v.GoalId == competition.GoalId && v.VoterId == player.Id && v.VotedAt >= startOfDay);
        return Math.Max(0, competition.VotesPerDay - cast);
    }

    private TimeZoneInfo HotelZone() =>
        hotel.CurrentValue.TimeZone.Length == 0 ? TimeZoneInfo.Local : TimeZoneInfo.FindSystemTimeZoneById(hotel.CurrentValue.TimeZone);
}
