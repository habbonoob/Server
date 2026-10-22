using System.Globalization;
using Habbo.Domain.Support;
using Habbo.Domain.Moderation;
using Habbo.Infrastructure;
using Habbo.Protocol.Composers;
using Habbo.Protocol.Messages.Outgoing.Help;
using Habbo.Silo.Abstractions.Achievements;
using Habbo.Silo.Abstractions.Moderation;
using Habbo.Silo.Abstractions.Players;
using Habbo.Silo.Chat;
using Habbo.Silo.Help;
using Habbo.Silo.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Orleans.Runtime;

namespace Habbo.Silo.Grains.Help;

/// <summary>The hotel's guide desk. Guides go on duty for tours, help requests and bully reports; a request
/// is offered to one guide at a time until one takes it, and the two then talk until the guide is done or
/// the requester is; a bully report is judged by a panel of guardians on the chat around it, and what they
/// find goes back to the reporter and, when it stands or cannot be decided, to the moderators.</summary>
public sealed class GuideDeskGrain(
    IDbContextFactory<AppDbContext> dbFactory,
    PlayerMessenger messenger,
    IModerationService moderation,
    IPerkService perks,
    WordFilter wordFilter,
    IOptions<GuideOptions> options) : Grain, IGuideDeskGrain
{
    /// <summary>The longest line a guide session keeps, as long as a line of room chat.</summary>
    private const int MaxLineLength = RoomChatLine.TextMaxLength;

    /// <summary>The lines of a session kept for a report: the latest, as a moderator reads the end of it first.</summary>
    private const int MaxTranscriptLines = 200;

    private const int TourRequest = 0;

    /// <summary>The perks that make a guide, who gives tours and help, and a guardian, who judges bully reports.</summary>
    private const string GuidePerk = "USE_GUIDE_TOOL";
    private const string GuardianPerk = "JUDGE_CHAT_REVIEWS";

    private const string ChatRecordTimeFormat = "yyyy-MM-dd HH:mm:ss";
    private const string ChatRecordSeparator = "\r";

    /// <summary>The suspect is speaker 0 in a chat record; everyone else is numbered from 1.</summary>
    private const int SuspectSpeaker = 0;

    /// <summary>How long the desk stays up with nothing going on after the last call.</summary>
    private static readonly TimeSpan KeptWhileBusy = TimeSpan.FromHours(12);

    private readonly Dictionary<Guid, GuideDuty> _onDuty = [];
    private readonly List<GuideRequest> _requests = [];

    /// <summary>The last session each requester had, for their feedback and report afterwards.</summary>
    private readonly Dictionary<Guid, GuideRequest> _finished = [];

    private readonly List<ChatReview> _reviews = [];

    private GuideOptions Rules => options.Value;

    private void KeepAlive() => DelayDeactivation(KeptWhileBusy);

    private Task SendAsync(Guid playerId, IComposer composer) => messenger.SendAsync(playerId, composer);

    /// <summary>Moves a player on towards an achievement, without waiting: the desk is called by the player's
    /// grain as they go offline, and waiting on that grain from there would wait on itself.</summary>
    private void Progress(Guid playerId, string achievement, int amount)
    {
        if (achievement.Length > 0 && amount > 0)
            GrainFactory.GetGrain<IPlayerGrain>(playerId).ProgressAchievementAsync(achievement, amount).Ignore();
    }

    // ---------- duty ----------

    public async Task UpdateDutyAsync(PlayerData player, bool onDuty, bool tours, bool helps, bool chatReviews)
    {
        KeepAlive();

        // only a guide gives tours and help, and only a guardian sees bully reports; neither is not on duty at all
        var guide = (tours || helps) && await perks.IsAllowedAsync(player, GuidePerk);
        (tours, helps) = (tours && guide, helps && guide);
        chatReviews = chatReviews && await perks.IsAllowedAsync(player, GuardianPerk);
        onDuty = onDuty && (tours || helps || chatReviews);

        if (onDuty)
        {
            var since = _onDuty.TryGetValue(player.Id, out var already) ? already.Since : DateTimeOffset.UtcNow;
            _onDuty[player.Id] = new GuideDuty(player, tours, helps, chatReviews, since);
        }
        else
            await GoOffDutyAsync(player.Id);

        await SendDutyStatusAsync(player.Id);

        // someone new on duty may be what a waiting request needs
        foreach (var request in _requests.Where(r => r.Guide is null && r.Offered is null).ToList())
            await OfferAsync(request);
    }

    private async Task GoOffDutyAsync(Guid playerId)
    {
        if (!_onDuty.Remove(playerId, out var duty))
            return;

        Progress(playerId, Rules.OnDutyAchievement, (int)(DateTimeOffset.UtcNow - duty.Since).TotalMinutes);
    }

    private Task SendDutyStatusAsync(Guid playerId) =>
        SendAsync(playerId, new GuideOnDutyStatusMessageComposer(_onDuty.ContainsKey(playerId),
            _onDuty.Values.Count(d => d.Tours), _onDuty.Values.Count(d => d.Helps), _onDuty.Values.Count(d => d.ChatReviews)));

    private bool Busy(Guid guideId) =>
        _requests.Any(r => r.Guide?.Id == guideId || r.Offered?.Id == guideId)
        || _reviews.Any(r => r.Seats.Any(s => s.Guardian?.Id == guideId && s.Status is ReviewSeat.Waiting or ReviewSeat.Searching));

    // ---------- requests ----------

    public async Task CreateRequestAsync(PlayerData player, int type, string description)
    {
        KeepAlive();
        if (_requests.Any(r => r.Requester.Id == player.Id || r.Guide?.Id == player.Id))
        {
            await SendAsync(player.Id, new GuideSessionErrorMessageComposer(GuideSessionErrorMessageComposer.Generic));
            return;
        }

        description = (await wordFilter.FilterAsync(Clip(description.Trim()))).Text;
        var request = new GuideRequest { Requester = player, Type = type, Description = description };
        _requests.Add(request);
        await SendAsync(player.Id, new GuideSessionAttachedMessageComposer(false, type, description, Rules.EstimatedWaitSeconds));
        await OfferAsync(request);
    }

    /// <summary>Offers the request to the next guide on duty who takes its kind, is free, and has not passed
    /// on it; with nobody left, the requester is told nobody could take it.</summary>
    private async Task OfferAsync(GuideRequest request)
    {
        var guide = _onDuty.Values
            .Where(d => d.Guide.Id != request.Requester.Id && !request.Passed.Contains(d.Guide.Id) && !Busy(d.Guide.Id)
                        && (request.Type == TourRequest ? d.Tours : d.Helps))
            .OrderBy(d => d.Since)
            .Select(d => d.Guide)
            .FirstOrDefault();
        if (guide is null)
        {
            _requests.Remove(request);
            await SendAsync(request.Requester.Id, new GuideSessionErrorMessageComposer(GuideSessionErrorMessageComposer.Rejected));
            return;
        }

        request.Offered = guide;
        request.OfferTimer = this.RegisterGrainTimer(async _ => await PassAsync(request, guide.Id),
            new GrainTimerCreationOptions { DueTime = TimeSpan.FromSeconds(Rules.OfferTimeoutSeconds), Period = Timeout.InfiniteTimeSpan });
        await SendAsync(guide.Id, new GuideSessionAttachedMessageComposer(true, request.Type, request.Description, Rules.OfferTimeoutSeconds));
    }

    /// <summary>The guide offered the request passes on it, or let it lapse: it goes to the next one.</summary>
    private async Task PassAsync(GuideRequest request, Guid guideId)
    {
        if (request.Offered?.Id != guideId || !_requests.Contains(request))
            return;

        request.OfferTimer?.Dispose();
        request.Offered = null;
        request.Passed.Add(guideId);
        await SendAsync(guideId, new GuideSessionDetachedMessageComposer());
        await OfferAsync(request);
    }

    public async Task GuideDecidesAsync(PlayerData player, bool accept)
    {
        KeepAlive();
        if (_requests.FirstOrDefault(r => r.Offered?.Id == player.Id) is not { } request)
            return;

        if (!accept)
        {
            await PassAsync(request, player.Id);
            return;
        }

        request.OfferTimer?.Dispose();
        request.Offered = null;
        request.Guide = player;
        request.StartedAt = DateTimeOffset.UtcNow;
        var started = new GuideSessionStartedMessageComposer(request.Requester.AccountId, request.Requester.Username, request.Requester.Figure,
            player.AccountId, player.Username, player.Figure);
        await SendAsync(request.Requester.Id, started);
        await SendAsync(player.Id, started);
    }

    private GuideRequest? SessionOf(Guid playerId) =>
        _requests.FirstOrDefault(r => r.Guide is not null && (r.Requester.Id == playerId || r.Guide.Id == playerId));

    public async Task RequesterCancelsAsync(PlayerData player)
    {
        if (_requests.FirstOrDefault(r => r.Requester.Id == player.Id) is not { } request)
            return;

        if (request.Guide is null)
        {
            _requests.Remove(request);
            request.OfferTimer?.Dispose();
            if (request.Offered is { } offered)
                await SendAsync(offered.Id, new GuideSessionDetachedMessageComposer());
            await SendAsync(player.Id, new GuideSessionDetachedMessageComposer());
            return;
        }

        await EndAsync(request, endedBy: player.Id);
    }

    public async Task ResolveAsync(PlayerData player)
    {
        if (SessionOf(player.Id) is { } request && request.Guide!.Id == player.Id)
            await EndAsync(request, endedBy: player.Id);
    }

    /// <summary>Ends a session: each side is told who ended it, the help asked and given and the time on tour count.</summary>
    private async Task EndAsync(GuideRequest request, Guid endedBy, bool left = false)
    {
        _requests.Remove(request);
        _finished[request.Requester.Id] = request;
        var guide = request.Guide!;
        foreach (var side in new[] { request.Requester.Id, guide.Id })
        {
            var reason = left ? GuideSessionEndedMessageComposer.PartnerLeft
                : side == endedBy ? GuideSessionEndedMessageComposer.EndedByYou
                : GuideSessionEndedMessageComposer.EndedByPartner;
            if (!(left && side == endedBy))
                await SendAsync(side, new GuideSessionEndedMessageComposer(reason));
        }

        // counted only once both sides said something: a request nobody could take, or one taken and ended
        // at once again and again, counts for nothing
        var spoke = request.Transcript.Select(l => l.AccountId).ToHashSet();
        if (spoke.Contains(request.Requester.AccountId) && spoke.Contains(guide.AccountId))
        {
            Progress(request.Requester.Id, Rules.RequesterAchievement, 1);
            Progress(guide.Id, Rules.RequestHandlerAchievement, 1);
        }

        if (request.Type == TourRequest && request.StartedAt is { } started)
        {
            var minutes = (int)(DateTimeOffset.UtcNow - started).TotalMinutes;
            Progress(guide.Id, Rules.TourGiverAchievement, minutes);
            Progress(request.Requester.Id, Rules.TourTakerAchievement, minutes);
        }
    }

    public async Task SayAsync(PlayerData player, string message)
    {
        // said as room chat would be: not by the muted, no longer than a line, through the word filter
        if (SessionOf(player.Id) is not { } request || string.IsNullOrWhiteSpace(message) || player.MutedUntil > DateTimeOffset.UtcNow)
            return;

        message = (await wordFilter.FilterAsync(Clip(message.Trim()))).Text;
        request.Transcript.Add(new ReportedChatLine(player.AccountId, message));
        if (request.Transcript.Count > MaxTranscriptLines)
            request.Transcript.RemoveAt(0);
        var said = new GuideSessionMessageMessageComposer(message, player.AccountId);
        await SendAsync(request.Requester.Id, said);
        await SendAsync(request.Guide!.Id, said);
    }

    private static string Clip(string text) => text.Length > MaxLineLength ? text[..MaxLineLength] : text;

    public async Task TypingAsync(PlayerData player, bool typing)
    {
        if (SessionOf(player.Id) is { } request)
            await SendAsync(Partner(request, player.Id), new GuideSessionPartnerIsTypingMessageComposer(typing));
    }

    private static Guid Partner(GuideRequest request, Guid playerId) =>
        request.Requester.Id == playerId ? request.Guide!.Id : request.Requester.Id;

    /// <summary>The guide invites the requester to the room they are in; told so, or that they are in none.</summary>
    public async Task InviteRequesterAsync(PlayerData player)
    {
        if (SessionOf(player.Id) is not { } request || request.Guide!.Id != player.Id)
            return;

        var roomId = await GrainFactory.GetGrain<IPlayerGrain>(player.Id).GetCurrentRoomAsync() ?? 0;
        await using var db = await dbFactory.CreateDbContextAsync();
        var name = roomId == 0 ? string.Empty : await db.Rooms.Where(r => r.Id == roomId).Select(r => r.Name).FirstOrDefaultAsync() ?? string.Empty;
        var invite = new GuideSessionInvitedToGuideRoomMessageComposer(roomId, name);
        await SendAsync(player.Id, invite);
        if (roomId != 0)
            await SendAsync(request.Requester.Id, invite);
    }

    public async Task GoToRequesterRoomAsync(PlayerData player)
    {
        if (SessionOf(player.Id) is not { } request || request.Guide!.Id != player.Id)
            return;

        var roomId = await GrainFactory.GetGrain<IPlayerGrain>(request.Requester.Id).GetCurrentRoomAsync() ?? 0;
        await SendAsync(player.Id, new GuideSessionRequesterRoomMessageComposer(roomId));
    }

    public async Task FeedbackAsync(PlayerData player, bool recommend)
    {
        if (!_finished.Remove(player.Id, out var session) || session.Guide is not { } guide)
            return;

        Progress(player.Id, Rules.FeedbackGiverAchievement, 1);
        if (recommend)
            Progress(guide.Id, Rules.RecommendationAchievement, 1);
        await SendAsync(player.Id, new GuideSessionDetachedMessageComposer());
    }

    /// <summary>The requester reports their guide: the moderators get it with what was said.</summary>
    public async Task ReportGuideAsync(PlayerData player, string message)
    {
        var session = _finished.GetValueOrDefault(player.Id) ?? SessionOf(player.Id);
        if (session?.Guide is not { } guide || session.Requester.Id != player.Id)
            return;

        _finished.Remove(player.Id);
        await moderation.ForwardAsync(player.Id, new CallForHelpReport(ModerationIssueSources.GuideSystem, await ReportTopicAsync(), message,
            guide.AccountId, RoomId: null, session.Transcript.ToList()));
        await SendAsync(player.Id, new GuideSessionDetachedMessageComposer());
    }

    private async Task<int> ReportTopicAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.CallForHelpTopics.Where(t => t.Name == Rules.ReportTopicName).Select(t => t.Id).FirstOrDefaultAsync();
    }

    // ---------- whether the player may ask ----------

    public async Task SendReportingStatusAsync(PlayerData player)
    {
        var now = DateTimeOffset.UtcNow;
        if (_requests.FirstOrDefault(r => r.Requester.Id == player.Id) is { } request)
        {
            await SendAsync(player.Id, new GuideReportingStatusMessageComposer(GuideReportingStatusMessageComposer.Pending, new PendingGuideTicketData(
                request.Type, (int)(now - request.CreatedAt).TotalSeconds, IsGuide: false,
                request.Guide?.Username ?? string.Empty, request.Guide?.Figure ?? string.Empty, request.Description, null)));
            return;
        }

        if (_reviews.FirstOrDefault(r => r.Reporter.Id == player.Id) is { } review)
        {
            await using var names = await dbFactory.CreateDbContextAsync();
            var reported = await names.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == review.ReportedId);
            var room = await names.Rooms.Where(r => r.Id == review.RoomId).Select(r => r.Name).FirstOrDefaultAsync();
            await SendAsync(player.Id, new GuideReportingStatusMessageComposer(GuideReportingStatusMessageComposer.Pending, new PendingGuideTicketData(
                PendingGuideTicketData.ChatReview, (int)(now - review.CreatedAt).TotalSeconds, IsGuide: false,
                reported?.Username ?? string.Empty, reported?.Figure ?? string.Empty, null, room ?? string.Empty)));
            return;
        }

        await SendAsync(player.Id, new GuideReportingStatusMessageComposer(await ReportingRefusalAsync(player.Id) ?? GuideReportingStatusMessageComposer.Ok, null));
    }

    /// <summary>Why the player may not report now: too many of their reports did not stand lately, or they
    /// reported too recently. Null when they may.</summary>
    private async Task<int?> ReportingRefusalAsync(Guid playerId)
    {
        var now = DateTimeOffset.UtcNow;
        await using var db = await dbFactory.CreateDbContextAsync();
        var since = now.AddDays(-Rules.InvalidReportsDays);
        var invalid = await db.ChatReviewRecords.CountAsync(r => r.ReporterId == playerId && r.CreatedAt >= since && r.Verdict == ReviewSeat.Ok);
        if (Rules.InvalidReportsBeforeBlocked > 0 && invalid >= Rules.InvalidReportsBeforeBlocked)
            return GuideReportingStatusMessageComposer.Blocked;

        var last = await db.ChatReviewRecords.Where(r => r.ReporterId == playerId).MaxAsync(r => (DateTimeOffset?)r.CreatedAt);
        return last is { } at && (now - at).TotalSeconds < Rules.ReportCooldownSeconds ? GuideReportingStatusMessageComposer.TooQuick : null;
    }

    // ---------- bully reports ----------

    public async Task CreateChatReviewAsync(PlayerData player, int reportedAccountId, int roomId)
    {
        KeepAlive();
        await using var db = await dbFactory.CreateDbContextAsync();
        if (await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.AccountId == reportedAccountId) is not { } reported)
            return;

        // the client asks before it offers the report, so these are only met by one that did not ask
        switch (await ReportingRefusalAsync(player.Id))
        {
            case GuideReportingStatusMessageComposer.Blocked:
                await SendAsync(player.Id, new GuideTicketCreationResultMessageComposer(GuideTicketCreationResultMessageComposer.Blocked));
                return;
            case { } refusal:
                await SendAsync(player.Id, new GuideReportingStatusMessageComposer(refusal, null));
                return;
        }

        // one report open at a time: the cooldown counts from a report's record, which a review only
        // writes once it is judged
        if (_reviews.Any(r => r.ReportedId == reported.Id || r.Reporter.Id == player.Id))
        {
            await SendAsync(player.Id, new GuideTicketCreationResultMessageComposer(GuideTicketCreationResultMessageComposer.AlreadyReported));
            return;
        }

        // only chat the reporter could have heard: the room they are in (the last they entered), or one
        // they were in while the chat that is reviewed was said
        var since = DateTimeOffset.UtcNow.AddMinutes(-Rules.ReviewChatMinutes);
        var lastRoom = await db.RoomVisits.AsNoTracking().Where(v => v.UserId == player.Id)
            .OrderByDescending(v => v.Id).Select(v => (int?)v.RoomId).FirstOrDefaultAsync();
        if (lastRoom != roomId && !await db.RoomVisits.AnyAsync(v => v.UserId == player.Id && v.RoomId == roomId && v.EnteredAt >= since))
        {
            await SendAsync(player.Id, new GuideTicketCreationResultMessageComposer(GuideTicketCreationResultMessageComposer.NoChat));
            return;
        }

        // the room's chat around the report, from the suspect's first line on
        var lines = (await db.RoomChatLines.AsNoTracking().Where(l => l.RoomId == roomId && l.SaidAt >= since)
            .OrderByDescending(l => l.Id).Take(Rules.ReviewChatLines).ToListAsync()).OrderBy(l => l.Id).ToList();
        var first = lines.FindIndex(l => l.UserId == reported.Id);
        if (first < 0)
        {
            await SendAsync(player.Id, new GuideTicketCreationResultMessageComposer(GuideTicketCreationResultMessageComposer.NoChat));
            return;
        }

        lines = lines.Skip(first).ToList();
        var speakers = new Dictionary<Guid, int> { [reported.Id] = SuspectSpeaker };
        foreach (var line in lines.Where(l => !speakers.ContainsKey(l.UserId)))
            speakers[line.UserId] = speakers.Count;
        var accounts = await db.Users.AsNoTracking().Where(u => speakers.Keys.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.AccountId);

        var review = new ChatReview
        {
            Reporter = player, ReportedId = reported.Id, ReportedAccountId = reported.AccountId, RoomId = roomId,
            Record = string.Join(ChatRecordSeparator, lines.Select(l =>
                $"{l.SaidAt.UtcDateTime.ToString(ChatRecordTimeFormat, CultureInfo.InvariantCulture)};{speakers[l.UserId]};{l.Text}")),
            Lines = lines.Select(l => new ReportedChatLine(accounts.GetValueOrDefault(l.UserId), l.Text)).ToList(),
        };
        for (var i = 0; i < Rules.GuardiansPerReview; i++)
            review.Seats.Add(new ReviewSeat());

        _reviews.Add(review);
        await SendAsync(player.Id, new GuideTicketCreationResultMessageComposer(GuideTicketCreationResultMessageComposer.Sent));
        Progress(player.Id, Rules.ChatReviewRequesterAchievement, 1);

        // too few guardians to judge it, and the moderators have it instead
        var guardians = Guardians(review).Count();
        if (guardians < Rules.MinimumGuardians)
        {
            _reviews.Remove(review);
            await SendAsync(player.Id, new GuideSessionErrorMessageComposer(GuideSessionErrorMessageComposer.NotEnoughGuardians));
            await ForwardAsync(review, "not enough guardians");
            await RecordAsync(review, ReviewSeat.Refused);
            return;
        }

        foreach (var seat in review.Seats)
            await OfferSeatAsync(review, seat);
    }

    private IEnumerable<PlayerData> Guardians(ChatReview review) =>
        _onDuty.Values.Where(d => d.ChatReviews && d.Guide.Id != review.Reporter.Id && d.Guide.Id != review.ReportedId
                                  && !review.Asked.Contains(d.Guide.Id) && !Busy(d.Guide.Id))
            .OrderBy(d => d.Since).Select(d => d.Guide);

    /// <summary>Offers a seat to the next guardian free to take it; with nobody left, the seat is refused.</summary>
    private async Task OfferSeatAsync(ChatReview review, ReviewSeat seat)
    {
        if (Guardians(review).FirstOrDefault() is not { } guardian)
        {
            seat.Status = ReviewSeat.Refused;
            await MaybeConcludeAsync(review);
            return;
        }

        review.Asked.Add(guardian.Id);
        seat.Guardian = guardian;
        seat.Offered = true;
        seat.OfferTimer = this.RegisterGrainTimer(async _ => await DeclineSeatAsync(review, seat),
            new GrainTimerCreationOptions { DueTime = TimeSpan.FromSeconds(Rules.ReviewOfferTimeoutSeconds), Period = Timeout.InfiniteTimeSpan });
        await SendAsync(guardian.Id, new ChatReviewSessionOfferedToGuideMessageComposer(Rules.ReviewOfferTimeoutSeconds));
    }

    private async Task DeclineSeatAsync(ChatReview review, ReviewSeat seat)
    {
        if (!seat.Offered || !_reviews.Contains(review))
            return;

        seat.OfferTimer?.Dispose();
        seat.Offered = false;
        if (seat.Guardian is { } guardian)
            await SendAsync(guardian.Id, new ChatReviewSessionDetachedMessageComposer());
        seat.Guardian = null;
        await OfferSeatAsync(review, seat);
    }

    private (ChatReview Review, ReviewSeat Seat)? SeatOf(Guid guardianId)
    {
        foreach (var review in _reviews)
        {
            if (review.Seats.FirstOrDefault(s => s.Guardian?.Id == guardianId && s.Status is ReviewSeat.Waiting or ReviewSeat.Searching) is { } seat)
                return (review, seat);
        }

        return null;
    }

    public async Task ReviewDecidesAsync(PlayerData player, bool accept)
    {
        KeepAlive();
        if (SeatOf(player.Id) is not { Review: var review, Seat: { Offered: true } seat })
            return;

        if (!accept)
        {
            await DeclineSeatAsync(review, seat);
            return;
        }

        seat.OfferTimer?.Dispose();
        seat.Offered = false;
        seat.Status = ReviewSeat.Waiting;
        review.VotingTimer ??= this.RegisterGrainTimer(async _ => await VotingEndsAsync(review),
            new GrainTimerCreationOptions { DueTime = TimeSpan.FromSeconds(Rules.VotingTimeoutSeconds), Period = Timeout.InfiniteTimeSpan });
        await SendAsync(player.Id, new ChatReviewSessionStartedMessageComposer(Rules.VotingTimeoutSeconds, review.Record));
    }

    public async Task VoteAsync(PlayerData player, int vote)
    {
        if (SeatOf(player.Id) is not { Review: var review, Seat: { Offered: false, Status: ReviewSeat.Waiting } seat })
            return;

        seat.Status = vote switch
        {
            0 => ReviewSeat.Ok,
            1 => ReviewSeat.Bad,
            2 => ReviewSeat.VeryBad,
            _ => ReviewSeat.Refused,
        };
        await SendVotingStatusAsync(review);
        await MaybeConcludeAsync(review);
    }

    public async Task LeaveReviewAsync(PlayerData player)
    {
        if (SeatOf(player.Id) is not { Review: var review, Seat: var seat })
            return;

        seat.OfferTimer?.Dispose();
        seat.Offered = false;
        seat.Status = ReviewSeat.Refused;
        await SendAsync(player.Id, new ChatReviewSessionDetachedMessageComposer());
        await SendVotingStatusAsync(review);
        await MaybeConcludeAsync(review);
    }

    private Task SendVotingStatusAsync(ChatReview review) =>
        messenger.SendAsync(Seated(review), new ChatReviewSessionVotingStatusMessageComposer(review.Seats.Select(s => s.Status).ToList()));

    private static IEnumerable<Guid> Seated(ChatReview review) =>
        review.Seats.Where(s => s.Guardian is not null && !s.Offered).Select(s => s.Guardian!.Id);

    /// <summary>Time's up: whoever has not voted has refused.</summary>
    private async Task VotingEndsAsync(ChatReview review)
    {
        if (!_reviews.Contains(review))
            return;

        foreach (var seat in review.Seats.Where(s => s.Status is ReviewSeat.Waiting or ReviewSeat.Searching))
        {
            seat.OfferTimer?.Dispose();
            if (seat.Offered && seat.Guardian is { } offered)
                await SendAsync(offered.Id, new ChatReviewSessionDetachedMessageComposer());
            seat.Offered = false;
            seat.Status = ReviewSeat.Refused;
        }

        await MaybeConcludeAsync(review);
    }

    /// <summary>Once every seat has voted or refused: the verdict is the vote most guardians gave, and
    /// inconclusive on a tie or with no votes. The guardians see it, those who voted with it count it, the
    /// reporter hears what became of the report, and one that stands or cannot be decided goes to the moderators.</summary>
    private async Task MaybeConcludeAsync(ChatReview review)
    {
        if (!_reviews.Contains(review) || review.Seats.Any(s => s.Status is ReviewSeat.Waiting or ReviewSeat.Searching))
            return;

        _reviews.Remove(review);
        review.VotingTimer?.Dispose();

        var votes = review.Seats.Select(s => s.Status).Where(s => s is ReviewSeat.Ok or ReviewSeat.Bad or ReviewSeat.VeryBad)
            .GroupBy(s => s).Select(g => (Status: g.Key, Count: g.Count())).OrderByDescending(g => g.Count).ToList();
        var verdict = votes.Count == 0 || (votes.Count > 1 && votes[0].Count == votes[1].Count) ? ReviewSeat.Refused : votes[0].Status;

        var final = review.Seats.Select(s => s.Status).ToList();
        foreach (var seat in review.Seats.Where(s => s.Guardian is not null))
        {
            await SendAsync(seat.Guardian!.Id, new ChatReviewSessionResultsMessageComposer(verdict, seat.Status, final));
            // only a vote that agreed with a verdict counts: refusing, leaving or lapsing earns nothing
            if (verdict != ReviewSeat.Refused && seat.Status == verdict)
                Progress(seat.Guardian.Id, Rules.ChatReviewerAchievement, 1);
        }

        switch (verdict)
        {
            case ReviewSeat.Bad:
                await SendAsync(review.Reporter.Id, new GuideTicketResolutionMessageComposer(GuideTicketResolutionMessageComposer.Bullying));
                await ForwardAsync(review, "guardians found bullying");
                break;
            case ReviewSeat.VeryBad:
                await SendAsync(review.Reporter.Id, new GuideTicketResolutionMessageComposer(GuideTicketResolutionMessageComposer.SevereBullying));
                await ForwardAsync(review, "guardians found severe bullying");
                break;
            case ReviewSeat.Ok:
                await SendAsync(review.Reporter.Id, new GuideTicketResolutionMessageComposer(GuideTicketResolutionMessageComposer.Acceptable));
                break;
            default:
                await ForwardAsync(review, "guardians could not decide");
                break;
        }

        await RecordAsync(review, verdict);
    }

    private async Task ForwardAsync(ChatReview review, string finding) =>
        await moderation.ForwardAsync(review.Reporter.Id, new CallForHelpReport(ModerationIssueSources.Guardian, await ReportTopicAsync(),
            finding, review.ReportedAccountId, review.RoomId, review.Lines));

    private async Task RecordAsync(ChatReview review, int verdict)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        db.ChatReviewRecords.Add(new ChatReviewRecord
        {
            ReporterId = review.Reporter.Id, ReportedId = review.ReportedId, RoomId = review.RoomId, CreatedAt = review.CreatedAt, Verdict = verdict,
        });
        await db.SaveChangesAsync();
    }

    // ---------- leaving ----------

    public async Task PlayerLeftAsync(Guid playerId)
    {
        await GoOffDutyAsync(playerId);

        foreach (var request in _requests.Where(r => r.Requester.Id == playerId || r.Guide?.Id == playerId || r.Offered?.Id == playerId).ToList())
        {
            if (request.Offered?.Id == playerId)
                await PassAsync(request, playerId);
            else if (request.Guide is null)
            {
                _requests.Remove(request);
                request.OfferTimer?.Dispose();
                if (request.Offered is { } offered)
                    await SendAsync(offered.Id, new GuideSessionDetachedMessageComposer());
            }
            else
                await EndAsync(request, endedBy: playerId, left: true);
        }

        _finished.Remove(playerId);
        foreach (var (review, seat) in _reviews.SelectMany(r => r.Seats.Where(s => s.Guardian?.Id == playerId).Select(s => (r, s))).ToList())
        {
            seat.OfferTimer?.Dispose();
            if (seat.Offered)
            {
                seat.Offered = false;
                seat.Guardian = null;
                await OfferSeatAsync(review, seat);
                continue;
            }

            if (seat.Status == ReviewSeat.Waiting)
            {
                seat.Status = ReviewSeat.Refused;
                await SendVotingStatusAsync(review);
                await MaybeConcludeAsync(review);
            }
        }
    }
}
