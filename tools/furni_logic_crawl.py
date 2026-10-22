"""Crawls every furni SWF named in seed/furnidata.xml and records what the client reads from it:
the logic and visualization names (from <class>_index), model dimensions, allowed directions and
anything else the logic XML carries (from <class>_logic) and the visualization's animation ids
(from <class>_visualization), which are the item's states. Writes seed/furni_logic.json, keyed by base class name (colour variants such as
"rare_dragonlamp*4" share the SWF of "rare_dragonlamp").

Usage: python tools/furni_logic_crawl.py [--workers 24] [--base http://images.habbo.com/dcr/hof_furni]
Re-running resumes: classes already in the output file are skipped unless --refresh is given."""
import argparse
import json
import math
import re
import struct
import sys
import urllib.error
import urllib.request
import xml.etree.ElementTree as ET
import zlib
from concurrent.futures import ThreadPoolExecutor, as_completed
from pathlib import Path

SEED = Path(__file__).resolve().parent.parent / "seed"
# Animation ids at or above this are transitions between states, not states themselves.
TRANSITION_ID_START = 100


def fetch(url):
    req = urllib.request.Request(url, headers={"User-Agent": "Mozilla/5.0"})
    with urllib.request.urlopen(req, timeout=30) as r:
        return r.read()


def swf_body(swf):
    if swf[:3] == b"CWS":
        return zlib.decompress(swf[8:])
    if swf[:3] == b"FWS":
        return swf[8:]
    raise ValueError(f"not a SWF: {swf[:3]!r}")


def swf_tags(body):
    nbits = body[0] >> 3
    pos = (5 + nbits * 4 + 7) // 8 + 4
    while pos + 2 <= len(body):
        code_len = struct.unpack_from("<H", body, pos)[0]
        pos += 2
        code, length = code_len >> 6, code_len & 0x3F
        if length == 0x3F:
            length = struct.unpack_from("<I", body, pos)[0]
            pos += 4
        yield code, body[pos:pos + length]
        pos += length
        if code == 0:
            break


def xml_documents(body):
    """Maps symbol name -> XML text for every DefineBinaryData tag holding XML."""
    names, blobs = {}, {}
    for code, data in swf_tags(body):
        if code == 76:  # SymbolClass
            count, p = struct.unpack_from("<H", data, 0)[0], 2
            for _ in range(count):
                cid = struct.unpack_from("<H", data, p)[0]
                end = data.index(b"\0", p + 2)
                names[cid] = data[p + 2:end].decode("utf-8", "replace")
                p = end + 1
        elif code == 87:  # DefineBinaryData: [u16 id][u32 reserved][bytes]
            cid = struct.unpack_from("<H", data, 0)[0]
            payload = data[6:]
            if payload.lstrip()[:1] == b"<":
                blobs[cid] = payload
    return {names.get(cid, f"#{cid}"): blob for cid, blob in blobs.items()}


def find(docs, suffix):
    for name, blob in docs.items():
        if name.endswith(suffix):
            return ET.fromstring(blob)
    return None


def number(text):
    """A finite float, or None: some SWFs carry values like "NaN" that JSON cannot hold."""
    try:
        value = float(text)
    except (TypeError, ValueError):
        return None
    return value if math.isfinite(value) else None


def element(node):
    """An XML element as plain data: its attributes, plus its children by tag."""
    data = dict(node.attrib)
    for child in node:
        data.setdefault(child.tag, []).append(element(child))
    return data


def describe(cls, body):
    docs = xml_documents(body)
    index = find(docs, "_index")
    if index is None:
        raise ValueError("no _index document")
    entry = {"logic": index.get("logic"), "visualization": index.get("visualization")}

    logic = find(docs, "_logic")
    if logic is not None:
        dims = logic.find("model/dimensions")
        if dims is not None:
            entry["dimensions"] = {k: v for k in ("x", "y", "z") if (v := number(dims.get(k))) is not None}
        entry["directions"] = sorted(int(d.get("id")) for d in logic.findall("model/directions/direction"))

        # Anything the logic XML carries besides the model: a credit furni's worth, a planet
        # system's planets, and whatever else a behaviour reads. Kept verbatim so a new
        # behaviour does not need another crawl of 13k SWFs.
        extras = {child.tag: [element(node) for node in logic.findall(child.tag)]
                  for child in logic if child.tag != "model"}
        if extras:
            entry["logicData"] = extras

    viz = find(docs, "_visualization")
    if viz is not None:
        # Every size carries the same animation set; take the union to be safe.
        ids = {int(a.get("id")) for a in viz.iter("animation") if a.get("id") is not None}
        entry["states"] = sorted(i for i in ids if i < TRANSITION_ID_START)
        entry["transitions"] = sorted(i for i in ids if i >= TRANSITION_ID_START)
    return entry


def classes(furnidata):
    """Base class name -> highest revision among its variants, for floor and wall items."""
    root = ET.parse(furnidata).getroot()
    result = {}
    for furni in root.iter("furnitype"):
        base = furni.get("classname").split("*")[0]
        rev = int(furni.findtext("revision") or 0)
        result[base] = max(result.get(base, 0), rev)
    return result


def crawl(base_url, cls, rev):
    try:
        return cls, describe(cls, swf_body(fetch(f"{base_url}/{rev}/{cls}.swf"))), None
    except urllib.error.HTTPError as e:
        return cls, None, f"HTTP {e.code}"
    except Exception as e:  # noqa: BLE001 - recorded per class, the crawl carries on
        return cls, None, f"{type(e).__name__}: {e}"


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--base", default="http://images.habbo.com/dcr/hof_furni")
    ap.add_argument("--workers", type=int, default=24)
    ap.add_argument("--out", default=str(SEED / "furni_logic.json"))
    ap.add_argument("--refresh", action="store_true")
    ap.add_argument("--only", nargs="*", help="crawl just these base class names")
    args = ap.parse_args()

    out = Path(args.out)
    done = {} if args.refresh or not out.exists() else json.loads(out.read_text(encoding="utf-8"))
    todo = classes(SEED / "furnidata.xml")
    if args.only:
        todo = {c: r for c, r in todo.items() if c in args.only}
    else:
        todo = {c: r for c, r in todo.items() if c not in done}
    print(f"{len(done)} cached, {len(todo)} to crawl", file=sys.stderr)

    failures = {}
    with ThreadPoolExecutor(args.workers) as pool:
        futures = [pool.submit(crawl, args.base, c, r) for c, r in todo.items()]
        for n, fut in enumerate(as_completed(futures), 1):
            cls, entry, error = fut.result()
            if entry is not None:
                done[cls] = entry
            else:
                failures[cls] = error
            if n % 500 == 0:
                print(f"  {n}/{len(futures)} ({len(failures)} failed)", file=sys.stderr)
                out.write_text(json.dumps(dict(sorted(done.items())), indent=1, allow_nan=False), encoding="utf-8")

    out.write_text(json.dumps(dict(sorted(done.items())), indent=1, allow_nan=False), encoding="utf-8")
    print(f"wrote {len(done)} classes to {out}; {len(failures)} failed", file=sys.stderr)
    for cls, error in sorted(failures.items()):
        print(f"  {cls}: {error}", file=sys.stderr)


if __name__ == "__main__":
    main()
