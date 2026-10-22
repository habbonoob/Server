# Habbo.Tools

The hotel's tools, one command each:

```
dotnet run --project src/Habbo.Tools -- <command> [options]
```

## mirror

Mirrors a Habbo hotel's gamedata and the assets it names, for the server to serve its own.

It starts at `https://www.habbo.<hotel>/gamedata/hashes`, saves the gamedata files listed there (furnidata,
productdata, external_variables, external_texts, figuredata), with the ones served beside them under the same
hash (furnidata and productdata as json, the override texts and variables), and follows them the way the client
does:

| kind | what |
| --- | --- |
| `gamedata` | the files in the hashes manifest, each under its name and hash |
| `furni` | every furni's swf and icon: `hof_furni/<revision>/<class>.swf`, `<class>[_<colour>]_icon.png` |
| `figure` | `figuremap.xml` and every avatar library in it |
| `effects` | `effectmap.xml` and every effect library |
| `pets` | every pet library in `pet.configuration` |
| `badges` | every badge named in external_texts (`album1584`, gif, else png) |
| `images` | every image the gamedata names: landing view promos, reception and hotel view art, notification icons |
| `habbopages` | the in-client help pages, followed from link to link, kept as `gamedata/habbopages/<name>.txt`, and their images |
| `catalogue` | the catalogue page icons, `catalogue/icon_<n>.png`, up to `--max-icon` |
| `badgeparts` | the group badge parts, `Badgeparts/badgepart_<name>.png`, as `--badge-parts` (the seed) lists them |
| `sounds` | the sound machine samples by number, up to `--max-sample` (off unless asked for) |

Numbered files (catalogue icons, sound samples) are probed: a number the hotel does not have is no failure. The
catalogue's page headers and teasers are named only by the live catalog, not the gamedata, so they are not here.

```
dotnet run --project src/Habbo.Tools -- mirror --out ../habbo-assets
dotnet run --project src/Habbo.Tools -- mirror --only gamedata,furni --limit 50
dotnet run --project src/Habbo.Tools -- mirror --hotel com.br --workers 16
```

The gamedata comes first, as everything else is found from it; then every kind downloads at the same time, with a
progress bar each, sharing `--workers` downloads at a time between them (default 16).

Files keep their host and path under `--out`, so the folder can be served as it is. What is there already is
skipped, so a run can be stopped (Ctrl+C) and started again. The gamedata files are the exception: they are kept
as plain files under their names (`<host>/gamedata/furnidata.xml`, `external_texts.txt`, ...) and fetched again
only when their hash in the manifest changes.

What went wrong (errors, timeouts, refusals) is in `failed.txt`. What Habbo simply does not have (404: badges with
a name but no image, system furni at revision 0) is in `missing.txt`, apart from it.

The hotel sits behind DDoS protection that turns away requests that do not look like a browser's, so the mirror
sends a browser's headers. Keep `--workers` modest.

## release-headers

Reads a client release's message ids from its decompiled AS3 source into `src/Habbo.Protocol/Releases/<release>.json`:

```
dotnet run --project src/Habbo.Tools -- release-headers <client-src> [--out <dir>]
```

It takes the composers and events from `com/sulake/habbo/communication/HabboMessages.as`, by the client's class names,
and the release from `ClientHelloMessageComposer`. Classes the build still hides (`_SafeCls_*`) are left out.

Every release renumbers its messages. The server speaks the ids of `ReleaseHeaders.Native` (those of `ClientMessages`
and `ServerMessages`); a connection whose ClientHello names another release with a table here has its headers
translated by message name, both ways. A message only one of the two releases has is not read from that client, and
not sent to it.
