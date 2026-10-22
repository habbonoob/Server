# Packet logger

A [G-Earth](https://github.com/sirjonasxx/G-Earth) extension, built on
[xabbo/gearth](https://github.com/xabbo/gearth), that records every packet between the client and
Habbo and exposes a local HTTP API to **drive the client and read the answers**: send a packet
(walk, click furni, open a page), wait for the server's reply, mark sections, start new captures.
It exists so a capture of a specific feature can be taken by a script (or Claude) rather than by
hand, and later used to check the emulator's logic against real Habbo.

Like the catalog dumper it is not part of `Habbo.slnx`: it targets `net8.0` for the xabbo packages.

## Running it

```
dotnet run --project tools/Habbo.PacketLogger
```

Start G-Earth and connect it to Habbo; the extension connects to G-Earth on its own and keeps
retrying if G-Earth is not up yet or restarts. The green play button pauses and resumes recording.

| Variable | Default | Meaning |
| --- | --- | --- |
| `PACKETLOG_DIR` | `habbo-packetlogs/captures` at the repo root | Where captures go. |
| `PACKETLOG_PORT` | `8790` | The HTTP API's port (localhost only). |
| `PACKETLOG_BUFFER` | `5000` | How many recent packets `/recent` and `/wait` can see. |
| `PACKETLOG_TOKEN` | random each run (printed at startup) | The token `/send` wants in `X-Token`. |

## Captures

Each capture is two files named after its start time (and name, if given):

- `.txt` — G-Earth's export format (`[Name]`, `Incoming[id] -> ...`, `----`), minus the expression
  line, so `tools/achievements_from_capture.py` and friends read it. `--check <file>` proves the
  encoding matches G-Earth's byte for byte against an existing export.
- `.jsonl` — one packet per line: `seq`, `time`, `dir`, `id`, `name`, `length`, `hex` (the body,
  without length and header), and `blocked` when it was. Marker lines have `mark` instead. Both files are
  written to disk every second, so they can be followed while a capture runs. Two captures started in
  the same second get `-2`, `-3`, … on the second's name.

Packets the extension sends are recorded as `sent ...` markers.

## API

All routes but `/send` answer GET, so plain `curl` works. `/send` is a POST with the run's token
(printed at startup, or `PACKETLOG_TOKEN`) in an `X-Token` header, and any request with an
`Origin` header is refused, so a web page open in the browser cannot send packets. Filters for `/recent` and `/wait`: `dir=in|out`,
`name=<regex>`, `exclude=<regex>`, `id=<header>`, `since=<seq>`, `limit=<n>`, `text=1` (add the
G-Earth-escaped body).

| Route | Does |
| --- | --- |
| `/status` | Connection, hotel, capture path, counters. |
| `/send` | POST: sends a packet written as a G-Earth expression (body or `?e=`). `?wait=<regex>` also waits for a matching reply. |
| `/recent` | The last packets matching the filters; with `since`, the next ones after it, oldest first, so paging on with `since=<lastSeq>` misses none. |
| `/wait` | Long-polls (`timeout` seconds, default 30) for the first matching packets after `since`. |
| `/mark?label=` | Writes a section header into the capture. |
| `/capture?name=` | Starts a new pair of capture files. |
| `/pause`, `/resume`, `/flush` | Recording control. |

Expressions: `{out:Name}` or `{in:Name}` (or a numeric header id) first, then values —
`{i:int}`, `{s:"string"}`, `{b:true|false|byte}`, `{u:short}` (or `{h:}`), `{l:long}`, `{d:double}`, `{x:hex}`.
`--encode '<expression>'` prints what one encodes to without sending it. Names are the ones
G-Earth uses for the connected client.

```sh
# walk to 5,7 and wait for the room to report the movement
curl -s 'localhost:8790/send?wait=UserUpdate&timeout=5' -H "X-Token: $PACKETLOG_TOKEN" --data-binary '{out:MoveAvatar}{i:5}{i:7}'

# everything the server said since seq 1200, minus the noise
curl -s 'localhost:8790/recent?since=1200&dir=in&exclude=^(Ping|UserUpdate)$'
```

Sending is Flash/Unity only. **Mind Habbo's rate limits**: pace scripted sends the way the
catalog dumper does, or the account gets throttled.

## Chat commands

Typing these in the client is swallowed by the extension, not said out loud:
`/log <label>` marks a section, `/log new <name>` starts a new capture, `/log pause` / `/log resume`.
Only `/log` itself and `/log ` with a space count, so `/logout` is said as usual.
