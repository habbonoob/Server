# Relay

Lets the web client (habbo-pixi's client-next) play on Habbo's own hotels, and records every packet it sends and
receives, decoded, in the same capture format as [Habbo.PacketLogger](../Habbo.PacketLogger/README.md). Unlike the
logger it needs no G-Earth and no official client: the client records its own traffic.

Two jobs:

- **Game connection.** The browser client can only open WebSockets; Habbo's game servers speak TCP to the desktop
  client. `/game?host=&port=` carries the client's WebSocket to that host's TCP port and back, byte for byte. Only
  hosts on `AllowedHosts` (Habbo's hotels by default) are reached, and only from the client's own pages, so it is not
  an open proxy. The traffic is encrypted after the handshake; the relay never reads it.
- **Captures.** The client posts its decoded packets to `/capture`; they are written to
  `habbo-packetlogs/captures` as a `.txt` (G-Earth's export format, for `tools/achievements_from_capture.py` and
  friends) and a `.jsonl` (`seq`, `time`, `dir`, `id`, `name`, `length`, `hex`) pair. Names are the client's class
  names (`ClientHelloMessageComposer`), not G-Earth's.

Like the packet logger it is not part of `Habbo.slnx`.

## Running it

```
dotnet run --project tools/Habbo.Relay
```

Then open the client with `?relay=8791&capture=8791`, plus whatever points it at the hotel and the SSO ticket:

| Parameter | Meaning |
| --- | --- |
| `relay=8791` | Game servers off this machine are reached through the relay (a port here, or a `ws://` URL). |
| `capture=8791` | Packets go to the relay's capture API (a port here, or an `http://` URL). Works on our own server too. |
| `packets=1` | Also write them to the browser console (independent of `capture`). |
| `copier=8791` | The room copier (habbo-pixi's `room-copier`): `:copy` or `:copy wired` in a room copies it, and the relay saves it as the G-Earth copier would (`/roomcopy`). |

The client must also hold the hotel's RSA public key for the handshake to succeed; the relay does not touch it.

| Setting | Default | Meaning |
| --- | --- | --- |
| `Relay:Port` | `8791` | Listens on localhost only. |
| `Relay:AllowedHosts` | `*.habbo.com`, `*.habbo.com.br`, … | Where `/game` may connect: a host, or `*.domain`. |
| `Relay:AllowedOrigins` | the client's preview and dev servers | Pages allowed to use it. |
| `Relay:CaptureDirectory` | `habbo-packetlogs/captures` | Absolute, or under the repository root. |

Settings come from `appsettings.json` beside it or environment variables (`Relay__AllowedHosts__0=…`).

## API

| Route | Does |
| --- | --- |
| `/game?host=&port=` | WebSocket: the game connection. |
| `POST /capture` | A JSON array of `{ dir, id, name, hex, time }`, appended to the capture. |
| `/capture/new?name=` | Starts a new pair of capture files. |
| `/mark?label=` | Writes a section header into the capture. |
| `/status` | The capture's path and how many packets are in it. |
| `POST /roomcopy` | A room copy (`Habbo.Domain.Rooms.Copies.RoomCopy`) from the client's room copier: checked against the record and saved where the G-Earth copier saves its own (`Relay:RoomCopyDirectory`, else `ROOMCOPY_DIR`, else `Documents/Habbo room copies`), named and written as it writes them. |

Incoming packets the client has no parser for are not captured (the client records what its parsers read).
