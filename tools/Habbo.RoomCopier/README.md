# Room copier

A [G-Earth](https://github.com/sirjonasxx/G-Earth) extension, built on
[xabbo/gearth](https://github.com/xabbo/gearth) and xabbo's core, that copies the room you are in on
Habbo to a file, for our hotel to build again. `:copy` only reads what the client was already sent
when it entered the room; nothing is sent to Habbo, and the `:copy` you say never reaches it.
`:copy wired` also reads the wired boxes' settings (below), which takes asking Habbo for them.

Like the packet logger it is not part of `Habbo.slnx`.

The web client copies rooms itself too, without G-Earth: habbo-pixi's `room-copier`, opened with
`?relay=8791&copier=8791` (see [the relay](../Habbo.Relay/README.md)). Same commands, same files.

## Copying a room

```
dotnet run --project tools/Habbo.RoomCopier
```

Start G-Earth and connect it to Habbo; the extension connects to G-Earth on its own. Then **enter the
room** (the copier knows a room from its entry, so one you were already in when it started has to be
entered again) and either say `:copy` or press the extension's green play button in G-Earth. A
whisper only you see says what was copied and where it went.

### Wired

A box's settings are not in the room's packets: the client is sent them when someone opens the box,
and only if the room lets them read its wired. `:copy wired` opens each box in turn, as a double click
would (one every 0.4 seconds), takes the dialog Habbo answers with so it never shows, and keeps what
it says: the picked furni, text and numbers, sources, variables, an effect's delay, a condition's
quantifier and negation, a selector's filter. Then it asks for the room's variables: the boxes use
them by ids Habbo numbers apart from the furni, so the import finds them again by name. A room whose
wired you can't read is copied without it (the whisper says so), and so is one whose owner hides the
wired boxes: they are not sent at all.

The copier tells boxes from other furni by their class names, which the Flash client is not sent: it
reads them from the hotel's furnidata (`gamedata/furnidata_json`) when it connects.

| Variable | Default | Meaning |
| --- | --- | --- |
| `ROOMCOPY_DIR` | `Documents/Habbo room copies` | Where copies are saved. |
| `ROOMCOPY_DEBUG` | unset | `1` prints each packet name the first time it comes. |

The copier reads the handful of packets it needs itself (xabbo's room tracker reads every room packet,
and one Habbo has since changed stops it), and a packet it can't read is reported and passed over.

A copy is a JSON file (`Habbo.Domain.Rooms.Copies.RoomCopy`): the floor plan and door, the wall and
floor thickness, wall height and hidden walls, the wallpaper, floor and landscape, and every visible
furni with its furnidata id (and class name, when the client was sent one), where it stands or hangs,
and what it shows: a lamp's state, a mannequin's figure, a high-score board's lines. Limited-edition
numbers are left behind; a copy is not one of the run.

## Importing it

On the hotel's website, **Import room** (on the staff desk, for those with the `room.import`
permission, Administrator by default) takes the file and builds it as a room of yours:

- the floor plan becomes the room's own model, as the floor plan editor saves one, and must pass the
  editor's checks (up to 64 by 64, the door on a tile);
- every furni the hotel has is created, owned by you, in its place and showing what it showed;
- the furni the hotel doesn't have, and any standing outside the floor plan, are left out and
  listed by class name (or `floor:<id>` / `wall:<id>`);
- wired boxes the hotel implements are set up as they were, picking the new items and the room's
  new variables (found by name: the variable box that makes each, or the built-in of the same name);
  boxes that remember the furni they pick remember them as they now stand;
- each box's settings are checked as saving its dialog would check them: sources and delay as the
  dialog offers them, the box's own rules, and the permission a reward box needs. A box that fails
  is placed with nothing set and listed by class name.

The same import is `POST /api/rooms/import` with the copy as the body and a logged-in session.

Teleports come without their links: the client is never sent those.
