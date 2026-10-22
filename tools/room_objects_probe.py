"""Prints the floor items the server sends for a room, field by field.

Use it to compare our Objects/ObjectAdd rows against a capture from Habbo when an item
renders differently there than here.

Run with the host up: python tools/room_objects_probe.py [roomId]
"""
import struct
import sys

from protocol_probe import connect_and_login, drain_until, wstr

OPEN_FLAT_CONNECTION = 3463
OBJECTS = 1818


def read_stuff_data(m):
    """The stuff data as the client reads it: a header int, then the payload for that layout."""
    header = m.i32()
    kind = header & 0xFF
    if kind == 0:  # legacy: one string
        return {"type": kind, "flags": header >> 8, "value": m.s()}
    return {"type": kind, "flags": header >> 8, "value": "(unread layout)"}


def main():
    room_id = int(sys.argv[1]) if len(sys.argv) > 1 else 1
    conn = connect_and_login("admin")
    conn.send(OPEN_FLAT_CONNECTION, struct.pack(">i", room_id) + wstr("") + struct.pack(">i", -1))

    message = drain_until(conn, OBJECTS)

    # Read each field into its own name: Python evaluates the right of an assignment first,
    # so reading two fields in one statement would take them off the wire back to front.
    owners = {}
    for _ in range(message.i32()):
        owner_id = message.i32()
        owners[owner_id] = message.s()

    count = message.i32()
    print(f"room {room_id}: {count} floor item(s)")
    for _ in range(count):
        item = {
            "id": message.i32(),
            "classId": message.i32(),
            "x": message.i32(),
            "y": message.i32(),
            "rotation": message.i32(),
            "z": message.s(),
            "sizeZ": message.s(),
            "extra": message.i32(),
        }
        item["stuffData"] = read_stuff_data(message)
        item["expiry"] = message.i32()
        item["usagePolicy"] = message.i32()
        item["ownerId"] = message.i32()
        print("  " + "  ".join(f"{key}={value}" for key, value in item.items()))


if __name__ == "__main__":
    main()
