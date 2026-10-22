"""Turns a captured Achievements packet into seed/achievements.json.

The client builds its whole achievement dialog from one AchievementsEvent, so the list has to be
the hotel's real one. Rather than invent it, this reads a G-Earth capture of Habbo's own answer
and writes the definitions out.

What a capture can give is every achievement's identity — id, badge code, category, how many
levels it has, how the client files it — plus the requirement and reward of whichever level the
captured account happened to be on. Levels it was not on are simply not in the packet, so they
cannot be recovered from it; see the note printed at the end.

Usage: python tools/achievements_from_capture.py habbo-packetlogs/achievements.txt
"""
import json
import re
import sys
from pathlib import Path

# G-Earth prints printable bytes as themselves and everything else as [n].
TOKEN = re.compile(r"\[(\d+)\]|(.)", re.S)


def decode(text):
    """The byte string a capture line stands for."""
    out = bytearray()
    for number, char in TOKEN.findall(text):
        if number:
            out.append(int(number))
        else:
            # A literal character stands for the byte with its code point; the capture is
            # Latin-1 per byte, so a multi-byte character would mean the file was re-encoded.
            code = ord(char)
            if code > 255:
                raise ValueError(f"character {char!r} is not a single byte — re-save as UTF-8")
            out.append(code)
    return bytes(out)


class Reader:
    def __init__(self, data):
        self.data = data
        self.at = 0

    def take(self, count):
        if self.at + count > len(self.data):
            raise EOFError(f"wanted {count} byte(s) at {self.at}, {len(self.data) - self.at} left")
        chunk = self.data[self.at:self.at + count]
        self.at += count
        return chunk

    def i32(self):
        return int.from_bytes(self.take(4), "big", signed=True)

    def i16(self):
        return int.from_bytes(self.take(2), "big", signed=True)

    def boolean(self):
        return self.take(1)[0] != 0

    def string(self):
        return self.take(self.i16()).decode("utf-8", "replace")


def find_packet(path):
    """The byte string of the Achievements packet in the capture."""
    for line in Path(path).read_text(encoding="utf-8").splitlines():
        match = re.match(r"\s*Incoming\[(\d+)\]\s*->\s*(.*)$", line)
        if not match:
            continue
        data = decode(match.group(2))
        reader = Reader(data)
        reader.i32()                      # length prefix
        if reader.i16() != int(match.group(1)):
            continue                      # header did not line up; not the packet we want
        count = reader.i32()
        # An Achievements packet opens with a plausible count and ends on its default category.
        if 0 < count < 10000:
            return reader, count
    raise SystemExit("no Achievements packet found in that capture")


def main():
    if len(sys.argv) < 2:
        raise SystemExit(__doc__)

    reader, count = find_packet(sys.argv[1])
    entries = []
    for index in range(count):
        entry = {
            "achievementId": reader.i32(),
            "level": reader.i32(),
            "badgeId": reader.string(),
            "scoreAtStartOfLevel": reader.i32(),
            "scoreLimit": reader.i32(),
            "levelRewardPoints": reader.i32(),
            "levelRewardPointType": reader.i32(),
            "currentPoints": reader.i32(),
            "finalLevel": reader.boolean(),
            "category": reader.string(),
            "subCategory": reader.string(),
            "levelCount": reader.i32(),
            "displayMethod": reader.i32(),
            "state": reader.i16(),
        }
        entries.append(entry)

    default_category = reader.string()
    left = len(reader.data) - reader.at
    if left:
        raise SystemExit(f"parsed {count} entries but {left} byte(s) remain — the layout is off")

    achievements = []
    for entry in entries:
        # The badge belongs to the level, not the achievement: ACH_RoomEntry3 is level 3 of
        # ACH_RoomEntry, but ACH_Profile1 and ACH_Profile2 are two achievements both on level 1,
        # and ACH_VipParties1_Entry carries no level at all. So the badge is kept as given and
        # the name is only a label — the id is what identifies the achievement.
        badge = entry["badgeId"]
        name = re.sub(r"\d+$", "", badge) or badge
        if not badge:
            # Retired achievements come through with no badge and no category, and the client
            # drops them on arrival. Keeping them would only add rows nobody can ever see.
            continue

        known_level = {
            "level": entry["level"],
            "badgeCode": badge,
            "requiredProgress": entry["scoreLimit"],
            "rewardPoints": entry["levelRewardPoints"],
        }
        achievements.append({
            "achievementId": entry["achievementId"],
            "name": name,
            "category": entry["category"],
            "subCategory": entry["subCategory"],
            "displayMethod": entry["displayMethod"],
            "rewardPointType": entry["levelRewardPointType"],
            "state": entry["state"],
            "levelCount": entry["levelCount"],
            "knownLevels": [known_level],
        })

    achievements.sort(key=lambda a: a["achievementId"])
    out = Path(__file__).resolve().parent.parent / "seed" / "achievements.json"
    out.write_text(json.dumps(achievements, indent=2) + "\n", encoding="utf-8")

    partial = [a for a in achievements if a["levelCount"] > 1]
    print(f"wrote {len(achievements)} achievement(s) to {out}")
    print(f"default category in the capture: {default_category!r}")
    print(f"dropped {count - len(achievements)} entry(ies) with no badge — the client drops them too")
    print()
    print(f"{len(partial)} of them have more than one level, and a capture only shows the level")
    print("the captured account was on. The rest of their levels are not in the packet and are")
    print("not guessed here: each carries levelCount plus the one level that was observed.")


if __name__ == "__main__":
    main()
