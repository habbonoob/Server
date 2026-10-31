# Capture wishlist

Features the client supports that the server cannot implement faithfully yet, because the data they run on
is not in the gamedata, the client or the packet logs. Each needs a capture of real Habbo; the list says what
to look for.

| Feature | Packets | What is missing |
| --- | --- | --- |
| Quests | `quest.*` | habbo.com no longer answers `GetQuests` (25 October 2026) and nothing in its client opens them: retired there, so nothing to capture. |
| Daily tasks | `quest.dailytasks.*` | The pool (`seed/daily_tasks.json`) has the tasks habbo.com set us, with their repeats and rewards, and others inferred from their texts. Missing: what the "find" tasks (`FIND_STUFF`, `FIND_PET`, `WALK_OVER_STUFF`) look for, the real repeats and rewards of the rest, and the bonus task. |
| Reward track wired | Wired effects 58 "progress reward track" and 59 "reset reward track" | Which furni carries them: furnidata names none. Until then the Habboween track's `CUSTOM_WIRED_ACTION` tasks never count. |
| Community goals | `GetCommunityGoalProgress`, `CommunityGoalVote`, hall of fame | Goal codes, per-goal targets and prizes, how votes map to options. Part of the quest package. |
| Talent track | `GetTalentTrack`, `GetTalentTrackLevel`, `TalentLevelUp` | Switched off on habbo.com (`talent.track.enabled` unset, October 2026), so it cannot be captured there. The tasks (achievements) of each level and its reward perks and products; only the level titles are in the texts. |
| Mystery box | `ShowMysteryBoxWait`, `GotMysteryBoxPrize`, `CancelMysteryBoxWait` | How boxes and keys are handed out (the texts tie it to advent quests), the prize pool. |
| Treasure hunt (`wf15`) | `ProgressTreasureHunt`, `TreasureHuntUpdate/Fail/FirstWinner` | The step codes the server accepts, the level required, the reward. |
| Sound machines / trax | `sound.*` | Song data (sounds, layers, lengths) and the song disc catalogue. |
| Builders Club catalogue and placement | `BuildersClubPlaceRoomItem/WallItem`, `BuildersClubQueryFurniCount`, `BuildersClubPlacementWarning`, `ObjectRemoveConfirm`, `:pickallbc` | The Builders Club catalogue page tree (`GetCatalogIndex` with `BUILDERS_CLUB`) and which offers it lists. |
| New user room picker | `SelectInitialRoom` | What rooms 10, 11 and 12 ("Home Shiny Home", "Penumbra Penthouse", "Sunshine Lounge") contain: model, decoration, furniture. |
| Furni rent or buy-out | `GetRentOrBuyoutOffer`, `ExtendRentOrBuyout*` | Which furniture can be rented, for how long and at what price. |
| Special room effects | `SpecialRoomEffect` (0 rotate, 1 shake, 2 zoom, 3 disco) | What sets them off; the client's only lead is the unexplained chat command `:yyxxabxa`. |
| Pet chatter | `:mutepets` | What pets say on their own and when, which the command silences. |
| Fastest / longest time high score boards | `highscore_fastesttime*`, `highscore_longesttime*` | What time a game writes on them (the whole game, the clock, a finish) and for which team. |
| Costume hopper | `hween12_costume_hopper`, `Rooms:CostumeEffects` | Which avatar effects count as a Habboween costume; until listed, the costume hopper turns everyone away. |
| New user experience | `NewUserExperienceScriptProceed`, `NewUserExperienceGetGifts`, `NewUserExperienceGiftOffer`, `NewUserExperienceNotComplete`, `ElementPointer` | The script's steps and when each is sent, which interface elements it points at, and the gift options (the texts name a cat, a dog and a pig) with their product codes and thumbnails. |
| Hotel bots' menu commands | `BotSkillListUpdate`, `BotForceOpenContextMenu` (skills 7 in-client link, 8 NUX proceed, 14 navigator search) | Which bots carry these skills and the label and link or search each one holds. |
| Avatar use and number overlays | `UseObject`, `GamePlayerValue` | What the server sends them for: the client only animates the carried item's use and shows a number over the avatar. |
| Ban details | `BanInfo` | When it is sent instead of `UserBanned`, and what its leading short (the target) means. |
| Habbicon purchases and claims | `BuyHabbicon`, `BuyHabbiconCollection`, `ClaimHabbicon`, `FavoriteHabbicon`, `UnfavoriteHabbicon`, `SendHabbicon`, `TriggerHabbicon` | What Habbo answers each with (the server sends `UserHabbiconStatusChanged`, and `PurchaseOK` or `PurchaseError` for a purchase or claim), whether a set bought with some of it owned still costs the whole set's price, and how quickly Habbicons may be shown one after another. |
| Seasonal quest progress | `AcceptQuest`, `ActivateQuest`, `RejectQuest`, `Quest`, `QuestCompleted`, `QuestCancelled` | What a seasonal quest answers once taken on, moved on and done, and the quest ids, types and steps of an advent calendar's days (the xmas2013 seed has the texts' codes, with ids and types of ours). |
| SnowStorm arenas | `seed/snowwar_arenas.json`, Game2EnterArena | The official layouts of the five arenas (field types 8 to 12: Arctic Island, Dragon Top, Forbidden Forest, Fight Night, Skull Falls Assault): floor, furni, trees, piles, machines and starting tiles. One arena made for this hotel stands in until then. |
