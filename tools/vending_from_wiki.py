"""Writes seed/vending.json, what each dispensing furni hands out, from HabboxWiki's list of hand items per furni.

Habbo's furnidata used to carry a vending machine's hand items in its custom params; it no longer does (every
vending_machine there has none), and many dispensers are not in that category at all: the Army compass and the Area
bookcase are "other" and "shelf" to furnidata, plain furniture the client lets you click. The wiki lists them by the
name the catalogue gives them and a picture, under the furni line they belong to:

    === Area ===                    the line
    ==== Safe Minibar ====          the furni, as the catalogue names it
    [[File:Area_safe.gif]]          often the class name itself
    | 17 | Java                     the hand items (fx_<n>: an avatar effect instead)

A table is matched to class names by its picture first (a file named after the class, every colour of it), then by
its heading, as furnidata names it or as the class itself (the ad furniture is listed by class). Never by the
table's own title: the wiki copies titles from table to table. A name several furni share is narrowed to those of
the section's line. The NFT copy of a furni dispenses as the furni does. What is left ambiguous or unmatched must be
settled in OVERRIDES or SKIPPED before anything is written, and where furnidata still has custom params they must
agree with the wiki.

The wiki does not say which colour of a several-coloured dispenser gives which item, so each colour gives any of
them, as one machine with several items does.

Save the page's wikitext first (https://habboxwiki.com/List_of_hand_items_per_furni?action=raw).

Usage: python tools/vending_from_wiki.py <saved wikitext> <furnidata.xml> [seed/vending.json] [--report=<file>]\n\n--report writes every table, how it was matched (picture, heading, class, override) and the furni it\nlanded on, a line each, for reading through after the page or OVERRIDES change.
"""
import json
import re
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

HEADING = re.compile(r"^(={2,5})\s*(.*?)\s*\1$")
IMAGE = re.compile(r"\[\[(?:File|Image):([^\]|]+)", re.I)
MARKUP = re.compile(r"<[^>]+>|'''|''|\[\[|\]\]")
EFFECT = re.compile(r"^fx_(\d+)$")

# Tables a picture, heading or title can't settle: the heading (or "Line / Heading" where a heading is used twice)
# and the classes it means.
OVERRIDES = {
    # furnidata calls three bookcases "Bookcase"; Area is the silo line, as its safe minibar is
    "Area / Bookcase": ["shelves_silo"],
    # its picture, Torch.png, is also the Gothic Torch's class
    "Habbo-Lympix Torch": ["track12_mini_torch"],
    "Diner / Gumball Machine(s)": ["diner_gumvendor"],
    "Habbo Palooza / Java Cafes": ["hblooza14_cafe_y", "hblooza14_cafe_b", "hblooza14_cafe_p"],
    "Slurpees": ["ads_711"],
    "Moodi Machine": ["ads_cl_moodi"],
    "Juice Dispensers": ["bonusrare18_1"],
    "Espresso Machines": ["bonusrare20_3"],
    "Pink Hyacinth": ["hyacinth1"],
    "Fruit Bowls": ["eco_fruits1", "eco_fruits2", "eco_fruits3"],
    "Calippo Slurpee Machine": ["ads_calip_cola"],
    "Area / Safe Minibar": ["safe_silo"],
    # furnidata names all three, and the wiki says the altar piece, not which
    "Day of the Dead Altar Piece": ["hween14_altarpieces1", "hween14_altarpieces2", "hween14_altarpieces3"],
    # the second is the one that can't be traded
    "Poinsetta": ["joulutahti", "joulutahti_notrd"],
    "Ad Furniture / Quick Coffee Stop": ["ads_mall_coffeem"],
    "Bathroom / Sinks": ["bathroom_sinkmodule1", "bathroom_sinkmodule2", "bathroom_sinkmodule3", "bathroom_sinkmodule4"],
    # "all give you the same hand item": every ice cream maker there is, but not the miniature or the trophy
    "Ice Cream Machine(s)": ["rare_icecream", "rare_icecream_campaign", "rare_colourable_icecream",
                             "rare_blackrosegold_icecream", "exe_icecream", "xmas13_icecream", "lt_r26_icecream",
                             "nft_ff23_v7_icecream_bliss", "nft_h24_robotik_icm", "nft_h23_trippy_icm", "nft_h25_xmasicm"],
    # "the black and the white version gives both the same": the Dark Pura line's own classes
    "Pura Minibar": ["bar_basic", "purablk_c16_bar"],
    "Pura Refrigerator": ["fridge", "purablk_c16_fridge"],
    # the heading misspells it
    "Ditch the Label Equality Machinee": ["ads_dtlrare_rc"],
    "Plants / Vase of Flowers": ["giftflowers"],
    "Shalimar / Vase of Flowers": ["bolly_vase"],
    "Summer Bazaar / Courtyard Fountain": ["bazaar_r17_fountain"],
    "Shalimar / Elephant Minibar": ["bolly_phant"],
    "Christmas / Elephant Minibar": ["qt_xm10_elephant"],
    "Easter / Pear Tree": ["easter_c17_peartree"],
    "Ecotron / Pear Tree": ["eco_tree2"],
    "Christmas / Gingerbread House": ["xmas13_gingerbread"],
    "New Year / Party Bar": ["ny2015_bar"],
    "New Year / Drinks tray": ["ny2015_drktray"],
}

# Tables that name nothing furnidata has.
SKIPPED = {
}


def text_of(cell):
    return MARKUP.sub("", cell).strip()


def norm_name(value):
    return re.sub(r"[^a-z0-9]+", " ", (value or "").lower()).strip()


def norm_image(file):
    stem = re.sub(r"\.(gif|png|jpg)$", "", file.strip(), flags=re.I).lower()
    return re.sub(r"[\s-]+", "_", stem)


def base(classname):
    return classname.lower().split("*")[0]


def read_tables(wikitext):
    """Every table on the page: the headings above it, its title, its pictures and its rows."""
    heads = {}
    tables = []
    table = None
    for raw in wikitext.splitlines():
        line = raw.strip()
        heading = HEADING.match(line)
        if heading:
            level = len(heading.group(1))
            heads = {k: v for k, v in heads.items() if k < level}
            heads[level] = heading.group(2)
            continue
        if line.startswith("{|"):
            table = {"section": [heads[k] for k in sorted(heads)], "title": "", "images": [], "rows": [[]]}
            continue
        if table is None:
            continue
        if line.startswith("|}"):
            tables.append(table)
            table = None
        elif line.startswith("|-"):
            table["rows"].append([])
        elif line.startswith("!"):
            title = re.match(r'^!\s*colspan="\d+"[^|]*\|\s*(.*)$', line)
            if title:
                table["title"] = title.group(1).strip()
            table["rows"][-1].append(None)        # a header row holds no item
        elif line.startswith("|"):
            cell = re.sub(r'^\|\s*(colspan="\d+"[^|]*\|)?', "", line)
            if 'colspan="' in line:
                table["images"] += IMAGE.findall(line)
            else:
                # "| 58 || Cup of blood": two cells on one line
                table["rows"][-1] += [text_of(part) for part in cell.split("||")]
    return tables


def items_of(table):
    """The hand items and effects a table lists: its rows' first cells."""
    hand, effects = set(), set()
    for row in table["rows"]:
        if len(row) < 2 or None in row:
            continue
        first = row[0].split()[0] if row[0] else ""
        if first.isdigit():
            hand.add(int(first))
        elif EFFECT.match(first):
            effects.add(int(EFFECT.match(first).group(1)))
        else:
            raise SystemExit(f"{' / '.join(table['section'])}: can't read the item '{row[0]}'")
    return hand, effects


def read_furnidata(path):
    furni = []
    for node in ET.parse(path).getroot().iter("furnitype"):
        furni.append({
            "class": node.get("classname"),
            "name": (node.findtext("name") or "").strip(),
            "line": (node.findtext("furniline") or "").strip(),
            "params": (node.findtext("customparams") or "").strip(),
        })
    return furni


class Furnidata:
    def __init__(self, furni):
        self.by_base, self.by_name = {}, {}
        for f in furni:
            self.by_base.setdefault(base(f["class"]), []).append(f)
            self.by_name.setdefault(norm_name(f["name"]), []).append(f)

    def bases(self, names):
        """The class names, every colour of each, and their NFT copies."""
        found = []
        for name in names:
            for b in (name, "nft_" + name):
                found += self.by_base.get(b, [])
        return found

    def match(self, table):
        """How the table names its furni ("picture", "heading", "class") and the classes, or "ambiguous" and the
        candidates, or nothing. A table's title is not used: the wiki copies titles from table to table, and one led
        the rare ice cream makers' table to a different furni that happened to share it."""
        line = norm_name(table["section"][1]) if len(table["section"]) > 1 else ""
        for image in table["images"]:
            if norm_image(image) in self.by_base:
                return "picture", self.bases([norm_image(image)])
        heading = table["section"][-1]
        as_class = norm_image(heading)
        if as_class in self.by_base:
            return "class", self.bases([as_class])
        named = self.by_name.get(norm_name(heading), [])
        if not named:
            return None, []
        # the NFT copy goes with its furni, and a shared name with the furni of the section's line
        originals = {base(f["class"]) for f in named if not f["class"].startswith("nft_")} or {base(f["class"]) for f in named}
        if len(originals) > 1:
            in_line = {b for b in originals if any(line and norm_name(f["line"]).startswith(line[:4]) for f in self.by_base[b])}
            originals = in_line or originals
        if len(originals) > 1:
            return "ambiguous", sorted(originals)
        return "heading", self.bases(sorted(originals))

def key_of(table):
    section = table["section"]
    heading = section[-1]
    return heading, (f"{section[-2]} / {heading}" if len(section) > 1 else heading)


def write_report(report, path):
    """Every table, how it was matched and what it landed on, for reading through."""
    lines = []
    for qualified, how, given, matched in report:
        if how == "ambiguous":
            furni = "AMBIGUOUS " + ", ".join(matched)
        else:
            classes = sorted({base(f["class"]) for f in matched})
            furni = "; ".join(f"{c} ({next(f['name'] for f in matched if base(f['class']) == c)} | "
                              f"{next(f['line'] for f in matched if base(f['class']) == c)})" for c in classes)
        lines.append(f"{qualified}\t{how}\t{','.join(map(str, given))}\t{furni or 'NOTHING'}")
    Path(path).write_text("\n".join(lines) + "\n", encoding="utf-8")


def main(wikitext_path, furnidata_path, out_path="seed/vending.json", report_path=None):
    furnidata = Furnidata(read_furnidata(furnidata_path))
    tables = read_tables(Path(wikitext_path).read_text(encoding="utf-8"))

    hand, effects, problems, report = {}, {}, [], []
    for table in tables:
        heading, qualified = key_of(table)
        if qualified in SKIPPED or heading in SKIPPED:
            continue
        given_hand, given_effects = items_of(table)
        override = OVERRIDES.get(qualified, OVERRIDES.get(heading))
        how, matched = ("override", furnidata.bases(override)) if override else furnidata.match(table)
        report.append((qualified, how, sorted(given_hand) + [f"fx_{e}" for e in sorted(given_effects)], matched))
        if how == "ambiguous":
            problems.append(f"{qualified}: several furni have this name: {', '.join(matched)}")
            continue
        if not matched:
            problems.append(f"{qualified}: no furni found (pictures: {', '.join(table['images']) or 'none'})")
            continue
        for f in matched:
            if given_hand:
                hand.setdefault(f["class"], set()).update(given_hand)
            if given_effects:
                effects.setdefault(f["class"], set()).update(given_effects)

    # furnidata's own custom params, where it still has them, must be the wiki's too
    by_class = {f["class"]: f for fs in furnidata.by_base.values() for f in fs}
    for classname, ids in hand.items():
        params = {int(p) for p in by_class[classname]["params"].split(",") if p.strip().isdigit()}
        if params and not params <= ids:
            problems.append(f"{classname}: furnidata hands out {sorted(params)}, the wiki {sorted(ids)}")

    if report_path:
        write_report(report, report_path)
    if problems:
        print("\n".join(problems), file=sys.stderr)
        raise SystemExit(f"{len(problems)} table(s) to settle in OVERRIDES or SKIPPED; nothing written.")

    def section(name, dispensers):
        lines = [f"  {json.dumps(c)}: {json.dumps(sorted(ids))}" for c, ids in sorted(dispensers.items())]
        return f' "{name}": {{\n' + ",\n".join(lines) + "\n }"

    # a furni to a line, so a change reads as one
    Path(out_path).write_text("{\n" + section("handItems", hand) + ",\n" + section("effects", effects) + "\n}\n",
                              encoding="utf-8")
    print(f"{len(hand)} hand item and {len(effects)} effect dispenser(s) from {len(tables)} table(s) to {out_path}")


if __name__ == "__main__":
    arguments = [a for a in sys.argv[1:] if not a.startswith("--report=")]
    report = next((a.split("=", 1)[1] for a in sys.argv[1:] if a.startswith("--report=")), None)
    if len(arguments) not in (2, 3):
        raise SystemExit(__doc__)
    main(*arguments, report_path=report)
