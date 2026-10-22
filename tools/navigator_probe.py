"""Exercises the navigator end to end: metadata, favourites, search filters, the tag cloud,
room creation and the missing-room reply. Run against a live host."""
import struct
import sys

sys.path.insert(0, "tools")
from protocol_probe import Conn, connect_and_login, wstr  # noqa: E402

NEW_NAVIGATOR_INIT = 627
NEW_NAVIGATOR_SEARCH = 815
ADD_FAVOURITE = 3089
DELETE_FAVOURITE = 3512
GET_POPULAR_TAGS = 893
CREATE_FLAT = 1671
OPEN_FLAT = 3463

NAVIGATOR_METADATA = 3474
SEARCH_RESULT_BLOCKS = 1893
FAVOURITES = 3738
FAVOURITE_CHANGED = 484
POPULAR_TAGS_RESULT = 3064
FLAT_CREATED = 2216
NO_SUCH_FLAT = 2933


def drain_until(conn, header, timeout=6.0):
    """Reads until the wanted header turns up, so unrelated pushes don't break a check."""
    conn.sock.settimeout(timeout)
    while True:
        message = conn.recv()
        if message.u16() == header:
            return message


OFFICIAL_PICTURE_FLAG = 1


def read_room(message):
    """GuestRoomData, mirroring NavigatorSearchResultBlocksEventComposer.WriteGuestRoomData."""
    room_id = message.i32()
    name = message.s()
    message.i32()                       # owner account id
    message.s()                         # owner name
    message.i32()                       # door mode
    message.i32()                       # user count
    message.i32()                       # max users
    message.s()                         # description
    message.i32()                       # trade mode
    message.i32()                       # score
    message.i32()                       # ranking
    message.i32()                       # category id
    for _ in range(message.i32()):      # tags
        message.s()
    flags = message.i32()
    if flags & OFFICIAL_PICTURE_FLAG:
        message.s()                     # official picture reference
    return room_id, name


def read_blocks(message):
    """NavigatorSearchResultBlocksEvent -> [(blockCode, [(roomId, name), ...]), ...]"""
    message.s()  # the search code echoed back
    message.s()  # the filter echoed back
    blocks = []
    for _ in range(message.i32()):
        code = message.s()
        message.s()      # block text
        message.i32()    # action allowed
        message.u8()     # force closed
        message.i32()    # view mode
        rooms = [read_room(message) for _ in range(message.i32())]
        blocks.append((code, rooms))
    return blocks


def main():
    conn = connect_and_login("admin")

    # --- metadata and the starred-room list arrive together on init
    conn.send(NEW_NAVIGATOR_INIT)
    message = drain_until(conn, NAVIGATOR_METADATA)
    contexts = []
    for _ in range(message.i32()):
        code = message.s()
        links = [(message.i32(), message.s(), message.s(), message.s()) for _ in range(message.i32())]
        contexts.append((code, links))
    assert contexts, "navigator sent no contexts"
    print(f"metadata: {[c[0] for c in contexts]}")

    message = drain_until(conn, FAVOURITES)
    limit, count = message.i32(), message.i32()
    starting = [message.i32() for _ in range(count)]
    assert limit > 0, f"favourite limit should be positive, got {limit}"
    print(f"favourites on init: limit={limit} rooms={starting}")

    # --- star a room, then confirm it lists under the favourites search code
    conn.send(ADD_FAVOURITE, struct.pack(">i", 1))
    message = drain_until(conn, FAVOURITE_CHANGED)
    room_id, is_favourite = message.i32(), message.u8() == 1
    assert (room_id, is_favourite) == (1, True), f"unexpected favourite change: {room_id} {is_favourite}"
    print("starred room 1")

    conn.send(NEW_NAVIGATOR_SEARCH, wstr("favorites") + wstr(""))
    blocks = read_blocks(drain_until(conn, SEARCH_RESULT_BLOCKS))
    favourite_rooms = [room for _, rooms in blocks for room in rooms]
    assert any(r[0] == 1 for r in favourite_rooms), f"starred room missing from favourites: {blocks}"
    print(f"favourites search returned {favourite_rooms}")

    # --- unstar and confirm it drops out again
    conn.send(DELETE_FAVOURITE, struct.pack(">i", 1))
    message = drain_until(conn, FAVOURITE_CHANGED)
    assert (message.i32(), message.u8() == 1) == (1, False), "unstar was not confirmed"
    conn.send(NEW_NAVIGATOR_SEARCH, wstr("favorites") + wstr(""))
    blocks = read_blocks(drain_until(conn, SEARCH_RESULT_BLOCKS))
    assert not [room for _, rooms in blocks for room in rooms], f"favourites should be empty: {blocks}"
    print("unstarred room 1; favourites empty again")

    # --- the search box understands owner: and plain text
    conn.send(NEW_NAVIGATOR_SEARCH, wstr("hotel_view") + wstr("owner:admin"))
    blocks = read_blocks(drain_until(conn, SEARCH_RESULT_BLOCKS))
    assert [room for _, rooms in blocks for room in rooms], f"owner: search found nothing: {blocks}"
    print("owner: filter matched admin's rooms")

    conn.send(NEW_NAVIGATOR_SEARCH, wstr("hotel_view") + wstr("owner:nobody-by-this-name"))
    blocks = read_blocks(drain_until(conn, SEARCH_RESULT_BLOCKS))
    assert not [room for _, rooms in blocks for room in rooms], "owner: search should have found nothing"
    print("owner: filter correctly excludes a non-owner")

    # --- the tag cloud answers even when no room carries tags yet
    conn.send(GET_POPULAR_TAGS)
    message = drain_until(conn, POPULAR_TAGS_RESULT)
    tags = [(message.s(), message.i32()) for _ in range(message.i32())]
    print(f"popular tags: {tags}")

    # --- creating a room returns the id the client then opens
    conn.send(CREATE_FLAT,
              wstr("Probe Room") + wstr("made by the navigator probe") + wstr("model_a")
              + struct.pack(">iii", 0, 25, 0))
    message = drain_until(conn, FLAT_CREATED)
    new_id, new_name = message.i32(), message.s()
    assert new_id > 0 and new_name == "Probe Room", f"unexpected creation result: {new_id} {new_name}"
    print(f"created room {new_id} '{new_name}'")

    conn.send(NEW_NAVIGATOR_SEARCH, wstr("my_rooms") + wstr(""))
    blocks = read_blocks(drain_until(conn, SEARCH_RESULT_BLOCKS))
    owned = [room for _, rooms in blocks for room in rooms]
    assert any(r[0] == new_id for r in owned), f"new room missing from my_rooms: {owned}"
    print(f"new room appears under my rooms: {owned}")

    # --- a room that does not exist must be refused, not left hanging
    conn.send(OPEN_FLAT, struct.pack(">i", 999999) + wstr("") + struct.pack(">i", -1))
    message = drain_until(conn, NO_SUCH_FLAT)
    assert message.i32() == 999999, "NoSuchFlat reported the wrong room"
    print("missing room answered with NoSuchFlat")

    print("ALL NAVIGATOR CHECKS PASSED")


if __name__ == "__main__":
    main()
