"""Decodes the generated WireManifest string constant back into JSON, for eyeballing."""
import glob
import json
import re
import sys

path = glob.glob("../src/Habbo.Protocol/obj/**/WireManifest.g.cs", recursive=True)[0]
source = open(path, encoding="utf-8-sig").read()
literal = re.search(r'Json = "(.*)";', source, re.S).group(1)
entries = json.loads(literal.replace('\\\\', '\\').replace('\\"', '"'))

print("entries:", len(entries))
wanted = set(sys.argv[1:])
for entry in entries:
    if not wanted or entry.get("name") in wanted:
        print(json.dumps(entry, indent=1))
