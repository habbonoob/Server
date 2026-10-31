# Asset bundles

habbo-client loads every asset library as one **bundle**: its images packed into lossless WebP atlases, and a binary
manifest of where each image is and the library's documents. The web host converts libraries into bundles on request
and keeps them on disk.

## Routes

| Route | Converts |
|---|---|
| `GET /mirror/{host}/{path}.hab?format=bundle` | A library the mirror holds (furni, figure parts, effects, pets), fetched into the mirror first if it lacks it. |
| `GET /client/components/{component}?format=bundle` | A component of the client binary (`HabboRoomUICom`, `HabboWindowManagerCom`, …): the assets its manifest lists, from the client's extracted embeds. |
| `GET /client/classes/{path}?format=bundle` | What one class of the client binary embeds, without a manifest (`splash/PhotoSplashScreen`): its images and text files, named after their members. `&members=a,b` bundles only those (up to 16). |
| `GET /client/local/{library}?format=bundle` | A library shipped beside the client (`HabboRoomContent`, `TileCursor`, `PlaceHolderFurniture`, …). |

The client's own files come from two folders (`Bundles` settings): `ClientAssetsDirectory`, the release's extracted
embeds (`_assets`: `<Component>_manifest.bin`, the layouts and images it names), and `LocalIncludeDirectory`, its
`local_include` libraries as `.hab`. Converted bundles are kept under `<mirror root>/.bundles/`, keyed by the source
and the converter's version, and served from there afterwards.

## Sources

A library is a set of named **documents** (XML, JSON or text) and named **images**. Habbo's `.hab` comes in two kinds:

- **A sheet library** (furni, figure parts, cursors): `<name>.json` and `<name>.png`, a TexturePacker sheet. The JSON's
  `assets` map gives each image's offset (`x`, `y`) and, for one drawn from another, its `source` and `flipH`/`flipV`;
  its frames are keyed `<documentClass>_<asset>`. A trimmed frame is put back into the image it was trimmed from
  (`spriteSourceSize` within `sourceSize`): Habbo's offsets are from that image's top left. The JSON without `assets` and `spritesheet` is kept as the
  document `<name>`, with `paletteAssets` added: the names of the assets marked `usesPalette` (pets), drawn in a
  palette the client picks.
- **A converted Flash library** (room content): XML documents (`index`, `room_assets`, `room_visualization`, …) and one
  PNG per image.

A client component is its manifest's assets: `text/xml` ones are documents, `image/png` ones images, with the
manifest's `offset="x,y"` param as their offset.

## Format

Little-endian throughout. Strings are a u16 byte length and UTF-8.

```
"HCB\0"
u16 version (1)
u16 flags: 1 the manifest is zlib-deflated (always, from this converter)
u32 manifest length, as stored
u32 manifest length, inflated
manifest
u16 atlas count
  u32 length, the atlas (lossless WebP)
```

The manifest:

```
string  the library's name
u32 sprite count
  string  name
  u8      atlas
  u16     x, y, width, height     its rectangle in the atlas
  i16     offset x, offset y      as the source gives them (Habbo's own sign)
  u8      flags: 1 flipH, 2 flipV, 4 has an offset
u32 document count
  string  name
  u8      kind: 0 text, 1 XML, 2 JSON
  u32     byte length, then the UTF-8 text
```

An image drawn from another (`source`) is a sprite of its own over the same rectangle, with its own offset and flips:
the client never follows source chains. Atlases are at most 2048 × 2048, so every GPU takes them; a library that
does not fit one gets more.

## Notes

- Lossless WebP packs a library's images smaller than its PNGs (the body figure library: 29.5 KB against 52.9 KB), but
  not a library of small palette images: one atlas holds more than 256 colours, so WebP loses its palette (room
  content: 225 KB against 122 KB of PNGs). Atlases grouped by palette would win that back.
- Development settings, as environment variables: `Bundles__ClientSourceDirectory` (the decompiled `src`),
  `Bundles__LocalIncludeDirectory`.
