# Room games

How Habbo's furni games behave, measured on Habbo itself (October 2026) through the web client and
the relay's captures, in a room built for it ("Game Lab"). The captures are in `habbo-packetlogs/captures`
(`2026-10-22_17-21-34_banzai-solo-1`, the placing in `2026-10-22_17-02-13_banzai-arena`).

## Battle Banzai

Setup measured: a 4×5 arena of `bb_patch1` (room x 7–10, y 1–5), `bb_gate_r` at its edge, `bb_counter`,
`bb_score_r`, `bb_puck`. One player, pink (red) team.

### Clock

- The counter's state is the seconds left; it is `30` when placed. Double-clicking its play button sends
  `UseFurniture` with param 1, which starts the game (from 30 when it had run out).
- The counter counts down once a second (`ObjectDataUpdate`, `"29"`, `"28"`, … `"0"`).
- Tiles change on a 500 ms tick of their own, batched into one `ObjectsDataUpdate` per tick; a step shows on the
  next tick, not at once.

### Tiles

- A tile's state is `3 × team + level`: `0` off, `1` neutral and lit, then per team three levels, the last locked.
  Pink (red) is `3`, `4`, `5`. (The other teams follow at 6–8, 9–11, 12–14, by the client's visualization.)
- When the game starts every tile goes to `1` in one batch.
- Each step of a player onto a tile raises its level by one: `3` on the first step, `4` on the second, `5`
  (locked) on the third. Standing still does nothing; a locked tile does not change again.
- Each tile locked scores 1 point for the team (the scoreboard's state is the score: `"1"`, `"2"`, …).

### Filling

- Locking can close off an area: when `(8,2)` had its four direct neighbours `(8,1)`, `(9,2)`, `(8,3)`, `(7,2)`
  locked, it was locked in the same tick as the last of them (`053="5" 054="5"`), and the score rose by 2.
- The corners of that ring, `(7,1)` and `(7,3)`, were still at level 4: the area is closed off by the locked tiles
  around it orthogonally, the diagonals do not need to be locked.

### Puck

- Walking into the puck kicks it in the walking direction: it moved 5 tiles, one per update
  (`ObjectUpdate`), at growing intervals (0.10, 0.13, 0.17, 0.25, 0.50 s); its state counted the power down,
  `51`, `41`, `31`, `21`, `11`, then `11` at rest.
- It colours the tiles it crosses for the kicker's team (`(9,4)` and `(9,5)` went to `3` on the next tick), and
  leaves the arena: it kept going over plain floor until it stopped.

### End

- At 0 the tiles that were never stepped on (still `1`) go to `0`.
- The winning team's locked tiles then flash: all `0`, all `5`, every 500 ms, for several seconds.
- Tiles left at a lower level (`3`, `4`) keep their colour.
- A one-player, one-team game in one's own room gave no Banzai achievement (no tiles locked, no player, no
  winner progress); only the generic ones (walking, time in game rooms).

## Teams (all games)

- Teams are the room's, per colour, across game types: the red Freeze gate showed `1` when it was placed because
  the player was in red through the Banzai gate, and a new red Freeze scoreboard showed red's Banzai score (`8`).
- Stepping onto a gate while in a team leaves the team, whichever gate it is: `YouArePlayingGame` false,
  `AvatarEffect` 0, and the gates of that colour count down. Stepping onto a gate when in no team joins it:
  `YouArePlayingGame` true, the team's effect (red Freeze: 40), and every gate of the colour counts up. A gate's
  state is the number in its team. The join happens as the step onto the gate starts, before the avatar is on it.
- Starting any counter starts the room's game for all game types at once: starting the Freeze counter also reset
  every Banzai tile to `1`.

## Freeze

Setup measured: a 4×4 arena of `es_tile` (room x 7–10, y 8–11), five `es_box` on it, `es_gate_r`, `es_exit`,
`es_counter`, `es_score_r`. One player, red. Capture `2026-10-22_17-30-21_freeze-solo-1` (marks
`freeze-game-3`, `freeze-game-4`).

### Start

- Double-clicking the counter (`UseFurniture` param 1) starts it at 30, counting down once a second.
- Each player gets `GamePlayerValue` (user index, 3): three lives, shown over the avatar.
- The team's scoreboards go to 40 for one player (presumably 40 per player).
- Ice blocks go to `0` (whole), the exit to `1`.

### Throwing

- A throw is a double-click on a Freeze tile: `UseFurniture` (tile id, param 0). The snowball lands on the tile the
  player stands on whichever tile was clicked (clicking a block next to the player threw on the player's own tile).
- The tile shows the snowball at once: state `1000` (`state / 1000` is the visual state, `state % 1000` a delay in
  ms the client waits before showing it, `FurnitureIceStormLogic`). With a range power-up it was `2000`.
- It explodes exactly 2.0 s later, in a cross: the centre and each tile out to the range go to `11xxx`, the delay
  growing 100 ms per tile outwards (`11100` next to it, `11200` two away). Default range 1; after picking up power-up 2
  twice the range was 2. The cross stopped at the player standing in it (the far side of the player was not hit).

### Ice blocks and power-ups

- A block in the blast breaks into a power-up: its state becomes the power-up's type, after the blast's delay
  (`2100` → 2, `6100` → 6, `1100` → 1). The type is random per break (the same block gave 6, then 2).
- Stepping onto a broken block picks its power-up up: the block goes to `12000` (type + 10, the picked-up look), and
  `ACH_FreezePowerUp` gains 1. Like gates, this happens as the step starts.
- Seen: type 2 (twice; the next snowball was `2000` and reached two tiles), type 6 (the heart, presumably a life),
  type 1.
- At the end of the game unpicked power-ups go to type + 10 as well (`11000`, `12000`, `16000`).

### Being hit

- A player in a blast (own snowball included) loses a life (`GamePlayerValue` 3 → 2) and the team 10 points
  (40 → 30, both red scoreboards, Banzai's included).
- `AvatarEffect` 12 (frozen) with a 100 ms delay; 5 s later effect 49 (presumably protection), 5 s after that the
  team effect (40) again.

### End

- At 0 the Banzai tiles and the exit go to `0`; Freeze tiles keep their last state.
- A solo game gave no Freeze winner or player achievement; only `ACH_FreezePowerUp` for the pick-ups.

## The server

`RoomGames` runs every game in the room together; `BanzaiArena` and `FreezeArena` hold each game's rules, the
numbers read from furnidata's custom params where Habbo keeps them there (`es_tile`: effects, points, blast step,
lives; `es_box`: the power-up odds; `bb_puck`: the kick's reach). Tests: `RoomGameArenaTests`.

Played on our own hotel with an October 2026 client, two seeded players (capture
`2026-10-22_18-1*_dev-banzai-2`, `*_dev-freeze`): Banzai levels, locks, the fill, a blue step taking a red tile,
the end flash and the achievements; Freeze throws, bursts, a self hit and a hit on the other team (+10 / -10), the
freeze and protection effects, as above.

A game is of Freeze only when a Freeze counter (`es_`) started it: starting a Banzai counter beside a Freeze arena
resets it but sets up no lives, head start or snowballs.

What the server does that one player on Habbo could not show, and so is a guess until a game of two teams is
captured there:

- A step on another team's unlocked Banzai tile makes it the stepper's first level; locked tiles of any team stop a
  fill, and an area with one in it is not taken.
- Game achievements (Banzai tiles locked, played, won; Freeze played, won) count only in a game two teams or more
  started in; `ACH_FreezePowerUp` counts always, as measured.
- Freeze: a hit on another team wins the thrower's team the hit's points (10), one on a teammate costs it the
  teammate points (-5); the team starts on 40 per player.
- Freeze power-ups 3 (one more snowball at once), 4 (a diagonal burst), 5 (a burst in every direction), 7 (a shield,
  ten seconds) are the classic Freeze ones by number; only 1, 2 and 6 were seen.
- A Freeze player out of lives leaves the team for the exit, and a game with one team left ends.
- The puck stops at an avatar or anything it cannot be moved onto; whether Habbo's bounces was not seen.

## Football

Measured in the same room with `fball_ball` (custom params `6,1,0.5,0.5`), `fball_goal_b` and `fball_goal_r` (3×1,
custom params the team: 3 blue, 1 red), `fball_score_b` / `fball_score_r` (`furniture_hockey_score`, `3,99`) and a
`fball_c26_standlesscounter`. Captures `2026-10-22_18-46-*_football`, `*_goal-sides`.

### Kicking, dribbling and the trick

- A walk that **ends** on the ball kicks it the way the player walks (diagonally too), as far as the custom params'
  first value: 6 tiles, one per update (`ObjectUpdate`), facing the way it rolls (dir 0 north, 2 east, 4 south, 6
  west, whatever the ball's artwork has), its state the power left × 11: `55`, `44`, `33`, `22`, `11`, `11`. Each
  tile takes longer: 0.10, 0.13, 0.17, 0.25, 0.50 s. It starts as the step onto the ball starts.
- A walk that goes **on over** the ball dribbles it: it moves one tile ahead, state `11`.
- The trick: when that dribble puts the ball where the walk ends (from (8,7) to (8,5) over the ball on (8,6)), the
  player stops **on** the ball rather than kicking it (the avatar's height is the ball's, `1.0E-6`).
- Walking off a ball one stands on heels it back the other way, as far as a kick: from the ball on (8,5) to
  (8,4), the ball went (8,6) … (8,11).

### Bounces

- Off a wall (or anything it cannot move onto): back the other way, a tile shorter, at `11`: east at (11,12) at
  `33`, then (10,12) and (9,12) at `11`. Into a corner diagonally at `22`, back a tile at `11`. With an avatar on
  the tile it would bounce to, it stops where it is.
- A goal only takes a ball from its open side: at dir 0 a ball rolling south, at dir 4 one rolling north. From its
  back the ball goes back a tile at half the power it came with and stops (a goal at dir 0 hit from the south: the
  ball at (8,8) at `44` went back to (8,9) at `22`), and nothing scores.

### Goals

- From the open side the ball rolls into the goal's tile (`1.0E-6` high), and in the same tick the goal lights up
  (state `1`, back to `0` 2 s later), the scoreboards of the goal's team count one, and the kicker waves
  (`Expression` 1). The ball then comes back out a tile at half its power (in at `44`, out at `22`) and stops.
- `ACH_FootballGoalScored` (the kicker) and `ACH_FootballGoalScoredInRoom` count each goal, in a solo game in
  one's own room too (Habbo sent them about 10 s later).
- The scoreboards' buttons: `UseFurniture` param 1 takes a goal off, 2 adds one, 3 (a double-click) clears it;
  the custom params' second value is the most it shows.

### The server

`FootballPitch` (with `FootballInteraction`, `FootballGoalInteraction` and `HockeyScoreInteraction`); a game's
moves may face any direction (`anyDirection`) and go into a goal (`intoFurni`). Football does not need a game
running: Habbo scored without the counter on. What a running counter does to it was not measured.
