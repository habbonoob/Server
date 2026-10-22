"""Writes seed/pet_types.json from the pet assets the client itself downloads.

    python tools/pets_from_assets.py

Each pet type (external_variables' pet.configuration, in type-id order) has an asset at
pet.dynamic.download.url + pet.dynamic.download.name.template. Its visualization lists the
palettes it can be drawn in and the postures and gestures it can take; its breed names come from
external_texts (pet.breed.<type>.<palette>). A palette is sellable when it has a breed name.
"""
import json, os, re, urllib.request, zlib

ROOT = os.path.join(os.path.dirname(__file__), "..")
GAMEDATA = os.path.join(ROOT, "seed", "gamedata")


def properties(name):
    values = {}
    for line in open(os.path.join(GAMEDATA, name), encoding="utf-8"):
        key, sep, value = line.rstrip("\n").partition("=")
        if sep:
            values[key] = value
    return values


def asset_xml(url):
    data = urllib.request.urlopen(url).read()
    if data[:3] == b"CWS":
        data = data[:8] + zlib.decompress(data[8:])
    return data.decode("latin1")


def ids(pattern, text):
    seen = []
    for value in re.findall(pattern, text):
        if value not in seen:
            seen.append(value)
    return seen


def main():
    variables = properties("external_variables.txt")
    texts = properties("external_texts.txt")
    base = variables["pet.dynamic.download.url"]
    template = variables["pet.dynamic.download.name.template"]
    types = []
    for type_id, code in enumerate(variables["pet.configuration"].split(",")):
        xml = asset_xml(base + template.replace("%type%", code))
        palettes = {}
        for tag in re.findall(r'<palette [^>]*>', xml):
            attributes = dict(re.findall(r'(\w+)="([^"]*)"', tag))
            palette = int(attributes["id"])
            palettes[palette] = {
                "id": palette,
                "sellable": f"pet.breed.{type_id}.{palette}" in texts,
                "breed": int(attributes.get("breed", 0)),
                "colorTag": int(attributes.get("colortag", -1)),
                "tags": [t for t in attributes.get("tags", "").split(",") if t],
                "master": attributes.get("master") == "true",
            }
        layers = {}
        for layer, tag in re.findall(r'<layer id="(\d+)" tag="([^"]+)"', xml):
            layers.setdefault(tag, int(layer))
        # A kind whose palettes carry breeds names its breeds (pet.breed.<type>.<breed>), and the shop
        # sells each named breed in the first palette of it that colours the body; any other kind
        # names its palettes, each of which is a breed of its own.
        if any(p["breed"] > 0 for p in palettes.values()):
            body = min(layers.items(), key=lambda item: item[1])[0] if layers else ""
            for p in palettes.values():
                p["sellable"] = False
            for breed in sorted({p["breed"] for p in palettes.values() if p["breed"] > 0}):
                first = min((p for p in palettes.values() if p["breed"] == breed and body in p["tags"]), key=lambda p: p["id"], default=None)
                if first is not None and f"pet.breed.{type_id}.{breed}" in texts:
                    first["sellable"] = True
        else:
            for p in palettes.values():
                p["breed"] = p["id"]
        types.append({
            "id": type_id,
            "code": code,
            "palettes": [palettes[p] for p in sorted(palettes)],
            "layers": [{"id": layer, "tag": tag} for tag, layer in sorted(layers.items(), key=lambda item: item[1])],
            "postures": ids(r'<posture [^>]*id="([^"]+)"', xml),
            "gestures": ids(r'<gesture [^>]*id="([^"]+)"', xml),
        })
        print(type_id, code, len(palettes), "palettes")
    with open(os.path.join(ROOT, "seed", "pet_types.json"), "w", encoding="utf-8", newline="\n") as out:
        json.dump({"types": types}, out, indent=1)
        out.write("\n")


if __name__ == "__main__":
    main()
