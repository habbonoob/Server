"""Fills in achievement level thresholds from saved HabboxWiki pages.

A capture of Habbo's Achievements packet gives every achievement's identity but only the one
level the captured account was on, so most thresholds are missing. The wiki lists every level of
an achievement as a badge image plus its description, and the badge image is named after the
badge itself, which is what joins the two sources:

    <img src=".../ACH_PetLover2.gif">  "Pet Owner Level 2 - For having 3 pets owned by your Habbo."
                    ^ badge, hence achievement and level        ^ the threshold

The description is prose, so the number is found by matching the client's own localized sentence
for that badge, which carries a %limit% exactly where the requirement goes. Nothing is trusted
until it is checked: every level the capture already knows is compared against the wiki's reading
of the same level, and the tool refuses to write anything if they disagree too often.

Usage: python tools/achievement_levels_from_wiki.py <directory of saved pages> <external_flash_texts>
"""
import json
import re
import sys
from pathlib import Path

BADGE_IMAGE = re.compile(r'src="[^"]*/(ACH_[A-Za-z0-9_]+?)\.(?:gif|png)"', re.I)
ROW = re.compile(r"<tr.*?</tr>", re.S)
CELL = re.compile(r"<t[hd].*?</t[hd]>", re.S)
TAGS = re.compile(r"<[^>]+>")
LEVEL_TEXT = re.compile(r"\bLevel\s+(\d+)\b", re.I)

# Numbers that are part of the sentence rather than the requirement.
NOT_A_THRESHOLD = re.compile(r"\blevel\s+\d+\b", re.I)


def text_of(html):
    return re.sub(r"\s+", " ", TAGS.sub(" ", html)).strip()


def split_badge(badge):
    """The achievement stem and the level, the way the client splits a badge."""
    at = len(badge)
    while at > 0 and badge[at - 1].isdigit():
        at -= 1
    return badge[:at], int(badge[at:] or 1)


def read_pages(directory, templates):
    """Every (badge, level, threshold) the saved pages spell out."""
    found = {}
    for path in sorted(Path(directory).glob("*.html")):
        html = path.read_text(encoding="utf-8", errors="replace")
        for row in ROW.findall(html):
            badges = BADGE_IMAGE.findall(row)
            if len(badges) != 1:
                continue                      # a row about several badges says nothing definite
            badge = badges[0]

            # The description is whatever cell holds text; the other holds the image.
            description = " ".join(text_of(cell) for cell in CELL.findall(row)).strip()
            if not description:
                continue

            stem, badge_level = split_badge(badge)
            stated = LEVEL_TEXT.search(description)
            level = int(stated.group(1)) if stated else badge_level
            if stated and level != badge_level:
                continue                      # the row disagrees with itself; leave it alone

            threshold = read_threshold(templates, badge, stem, description)
            if threshold is not None:
                found.setdefault((stem, level), threshold)
    return found


def read_threshold(templates, badge, stem, description):
    """The requirement the description states, or None when it states none.

    Two readings, because neither is enough alone. The client's localization carries the same
    sentence with a %limit% where the number goes, which locates it exactly — but the wiki's
    wording has drifted from the current sentence, so that only matches sometimes. Falling back
    to the first number in the prose covers the rest and is right far more often than not.

    A sentence with no %limit% at all counts nothing — "For switching accessibility in room
    settings" — so any number in that row is incidental and the level is left unknown rather
    than guessed at. That is what keeps a year out of the data.
    """
    template = templates.get(badge) or templates.get(stem)
    if template is not None and "%limit%" not in template:
        return None

    if template is not None:
        pattern = "".join(
            r"(?P<limit>\d[\d,]*)" if part == "%limit%"
            else r"[IVXLC]+" if part == "%roman%"
            else re.escape(part)
            for part in re.split(r"(%limit%|%roman%)", template)
        )
        match = re.search(pattern, description, re.I)
        if match:
            return int(match.group("limit").replace(",", ""))

    # The level is written out in the row ("... Level 3 - ..."), so it is not the requirement.
    rest = re.sub(r"\blevel\s+\d+\b", " ", description, flags=re.I)
    numbers = re.findall(r"\b(\d[\d,]*)\b", rest)
    return int(numbers[0].replace(",", "")) if numbers else None


def read_templates(path):
    """The badge descriptions from the client's localization, by badge or achievement stem."""
    templates = {}
    for line in Path(path).read_text(encoding="utf-8", errors="replace").splitlines():
        if "=" not in line:
            continue
        key, value = line.split("=", 1)
        # The plain spelling is the one the wiki's text follows; "_al_" is a reworded variant.
        if key.startswith("badge_desc_") and not key.startswith("badge_desc_al_"):
            templates.setdefault(key[len("badge_desc_"):].removesuffix("_HHCA"), value)
    return templates


def main():
    if len(sys.argv) < 3:
        raise SystemExit(__doc__)

    templates = read_templates(sys.argv[2])
    print(f"read {len(templates)} description template(s) from the localization")
    wiki = read_pages(sys.argv[1], templates)
    print(f"read {len(wiki)} level(s) from the wiki")

    seed_path = Path(__file__).resolve().parent.parent / "seed" / "achievements.json"
    achievements = json.loads(seed_path.read_text(encoding="utf-8"))

    # Check the wiki's reading against every level the capture already knows.
    agree = disagree = 0
    mismatches = []
    for achievement in achievements:
        for level in achievement["knownLevels"]:
            stem, _ = split_badge(level["badgeCode"])
            theirs = wiki.get((stem, level["level"]))
            if theirs is None:
                continue
            if theirs == level["requiredProgress"]:
                agree += 1
            else:
                disagree += 1
                mismatches.append((level["badgeCode"], level["requiredProgress"], theirs))

    checked = agree + disagree
    print(f"checked {checked} level(s) the capture also knows: {agree} agree, {disagree} differ")
    for badge, ours, theirs in mismatches[:10]:
        print(f"    {badge:30} capture {ours:>8}   wiki {theirs:>8}")
    if checked and disagree / checked > 0.1:
        raise SystemExit("too many disagreements — the wiki reading is not reliable, nothing written")

    # The capture is first-hand, so it wins wherever the two overlap.
    added = 0
    for achievement in achievements:
        by_level = {level["level"]: level for level in achievement["knownLevels"]}
        stem, _ = split_badge(achievement["knownLevels"][0]["badgeCode"])
        for level in range(1, achievement["levelCount"] + 1):
            if level in by_level:
                continue
            threshold = wiki.get((stem, level))
            if threshold is None:
                continue
            by_level[level] = {
                "level": level,
                "badgeCode": f"{stem}{level}",
                "requiredProgress": threshold,
                "rewardPoints": 0,
            }
            added += 1
        achievement["knownLevels"] = [by_level[key] for key in sorted(by_level)]

    seed_path.write_text(json.dumps(achievements, indent=2) + "\n", encoding="utf-8")

    total = sum(a["levelCount"] for a in achievements)
    have = sum(len(a["knownLevels"]) for a in achievements)
    print(f"\nadded {added} level(s) from the wiki")
    print(f"{have} of {total} levels now on record ({have * 100 // total}%)")


if __name__ == "__main__":
    main()
