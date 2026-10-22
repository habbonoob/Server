"""Two real sessions in one room: presence, walking, chat and leaving must reach the other
player. Reuses protocol_probe's crypto and framing primitives."""
import json, random, socket, struct, time, urllib.request

from protocol_probe import Conn, Rc4, WEB, encrypt_type2, recover_type1, wstr


def login(username):
    req = urllib.request.Request(WEB + "/api/auth/login", data=json.dumps(
        {"username": username, "password": "password"}).encode(), headers={"Content-Type": "application/json"})
    ticket = json.load(urllib.request.urlopen(req))["ssoTicket"]

    c = Conn()
    c.send(4000, wstr("WIN63-202603212315-320263584") + wstr("FLASH25") + struct.pack(">ii", 6, 4), plain=True)
    c.send(2735, plain=True)
    m = c.recv(); assert m.u16() == 1142
    prime = int(recover_type1(bytes.fromhex(m.s())))
    gen = int(recover_type1(bytes.fromhex(m.s())))
    a = random.getrandbits(240)
    c.send(764, wstr(encrypt_type2(str(pow(gen, a, prime)).encode()).hex()), plain=True)
    m = c.recv(); assert m.u16() == 1038
    server_pub = int(recover_type1(bytes.fromhex(m.s())))
    key_hex = ("%x" % pow(server_pub, a, prime)).upper()
    if len(key_hex) % 2:
        key_hex = "0" + key_hex
    c.rc4_in = Rc4(bytes.fromhex(key_hex)); c.rc4_out = Rc4(bytes.fromhex(key_hex))

    c.send(2354, wstr(ticket) + struct.pack(">i", 0))
    m = c.recv(); assert m.u16() == 3847
    c.account = m.i32()
    c.send(1484)
    c.preferences = read_preferences(drain_until(c, 2809))  # closes the hotel-view batch
    return c


def read_preferences(m):
    ui, furni, trax = m.i32(), m.i32(), m.i32()
    old_chat, invites, camera = m.u8(), m.u8(), m.u8()
    ui_flags, style = m.i32(), m.i32()
    return {"volumes": (ui, furni, trax), "style": style, "oldChat": old_chat,
            "invitesIgnored": invites, "cameraFollowDisabled": camera}


def recv_timeout(c, seconds):
    c.sock.settimeout(seconds)
    try:
        return c.recv()
    except socket.timeout:
        return None


def drain_until(c, header, seconds=5):
    """Consumes frames until one with the header arrives; returns it (header already read)."""
    deadline = time.time() + seconds
    while time.time() < deadline:
        m = recv_timeout(c, 0.5)
        if m is not None and m.u16() == header:
            return m
    raise AssertionError(f"never received {header}")


def collect(c, header, seconds):
    """Every frame with the header that arrives within the window (header already read)."""
    out = []
    deadline = time.time() + seconds
    while time.time() < deadline:
        m = recv_timeout(c, 0.3)
        if m is not None and m.u16() == header:
            out.append(m)
    return out


def read_users(m):
    users = []
    for _ in range(m.i32()):
        web_id, name, motto, figure = m.i32(), m.s(), m.s(), m.s()
        index, x, y, z, direction, user_type = m.i32(), m.i32(), m.i32(), m.s(), m.i32(), m.i32()
        if user_type == 1:
            m.s(); m.i32(); m.i32(); m.s(); m.s(); m.i32(); m.u8()
        users.append({"webId": web_id, "name": name, "index": index, "pos": (x, y)})
    return users


def read_updates(m):
    updates = []
    for _ in range(m.i32()):
        index, x, y, z, head, body, status = m.i32(), m.i32(), m.i32(), m.s(), m.i32(), m.i32(), m.s()
        updates.append({"index": index, "pos": (x, y), "status": status})
    return updates


def search(c, search_code):
    """Runs a navigator search; returns {block code: [(room id, name, user count)]}."""
    c.send(815, wstr(search_code) + wstr(""))
    m = drain_until(c, 1893)
    m.s(); m.s()
    blocks = {}
    for _ in range(m.i32()):
        code, text, allowed, closed, mode = m.s(), m.s(), m.i32(), m.u8(), m.i32()
        rooms = []
        for _ in range(m.i32()):
            room_id, name, owner_id, owner = m.i32(), m.s(), m.i32(), m.s()
            door_mode, users, max_users, description = m.i32(), m.i32(), m.i32(), m.s()
            m.i32(); m.i32(); m.i32(); m.i32()          # trade mode, score, ranking, category
            for _ in range(m.i32()):
                m.s()                                  # tags
            m.i32()                                    # optional-groups bitmask
            rooms.append((room_id, name, users))
        blocks[code] = rooms
    return blocks


def enter(c, room_id):
    c.send(3176, struct.pack(">iii", room_id, 0, 1))
    drain_until(c, 3576)
    c.send(3463, struct.pack(">i", room_id) + wstr("") + struct.pack(">i", -1))
    users = read_users(drain_until(c, 2519))
    statuses = read_updates(drain_until(c, 3657))
    drain_until(c, 3001)
    return users, statuses


def main():
    room_id = 1

    a = login("admin")
    a_users, _ = enter(a, room_id)
    a_index = next(u["index"] for u in a_users if u["webId"] == a.account)
    print(f"admin in room as index {a_index}")

    b = login("tester")

    # tester owns no rooms, but the hotel tab must still list admin's room with live population
    assert search(b, "myworld_view").get("my_rooms") == [], "tester should own no rooms"
    popular = search(b, "hotel_view").get("popular", [])
    listed = {room[0]: room for room in popular}
    assert room_id in listed, f"tester cannot find the room in the hotel tab: {popular}"
    assert listed[room_id][2] == 1, f"live user count wrong: {listed[room_id]}"
    print(f"tester found '{listed[room_id][1]}' in the hotel tab with {listed[room_id][2]} user(s)")

    b_users, b_statuses = enter(b, room_id)
    names = sorted(u["name"] for u in b_users)
    assert names == ["admin", "tester"], f"newcomer should see both avatars, saw {names}"
    assert len(b_statuses) == 2, f"newcomer should get both postures, got {len(b_statuses)}"
    b_index = next(u["index"] for u in b_users if u["webId"] == b.account)
    print(f"tester in room as index {b_index}; sees {names}")

    arrived = read_users(drain_until(a, 2519))
    assert [u["name"] for u in arrived] == ["tester"], f"admin should be told about tester, got {arrived}"
    print("admin was told tester arrived")

    # admin walks; tester must watch every step
    a.send(2256, struct.pack(">ii", 8, 3))
    seen = [u for m in collect(b, 3657, 4.0) for u in read_updates(m) if u["index"] == a_index]
    steps = [u for u in seen if u["status"].startswith("mv ")]
    assert len(steps) >= 3 and seen[-1]["status"] == "", f"tester did not see admin walk: {seen}"
    print(f"tester saw admin walk {len(steps)} steps to {seen[-1]['pos']}")

    # tester chats; admin must hear it from tester's avatar
    b.send(1119, wstr("hi admin") + struct.pack(">ii", 0, 7))
    m = drain_until(a, 947)
    speaker, text = m.i32(), m.s()
    assert (speaker, text) == (b_index, "hi admin"), f"wrong chat: {speaker} {text}"
    print(f"admin heard tester: '{text}'")

    # an emoticon must resolve to a gesture from the seeded chat_gestures rows (1 = smile)
    b.send(1119, wstr("nice to meet you :)") + struct.pack(">ii", 0, 7))
    m = drain_until(a, 947)
    speaker, text, gesture = m.i32(), m.s(), m.i32()
    assert (speaker, gesture) == (b_index, 1), f"wrong gesture for ':)': {speaker} {gesture} in '{text}'"
    print(f"emoticon ':)' resolved to gesture {gesture}")

    # plain text must resolve to no gesture
    b.send(1119, wstr("no emoticon here") + struct.pack(">ii", 0, 7))
    m = drain_until(a, 947)
    speaker, text, gesture = m.i32(), m.s(), m.i32()
    assert gesture == 0, f"expected no gesture for plain text, got {gesture}"
    print("plain text resolved to no gesture")

    # tester leaves; admin must see the avatar removed
    b.send(2780)
    b.sock.close()
    m = drain_until(a, 3842)
    removed = int(m.s())
    assert removed == b_index, f"removed the wrong avatar: {removed}"
    print(f"admin saw tester (index {removed}) leave")

    # preferences survive a re-login (trax, furni, ui is the client's wire order)
    p = login("tester")
    p.send(2565, struct.pack(">iii", 11, 22, 33))
    p.send(3760, struct.pack(">i", 4))
    p.send(3271, bytes([1]))
    time.sleep(0.5)
    p.sock.close()
    saved = login("tester").preferences
    assert saved["volumes"] == (33, 22, 11), f"volumes not saved in order: {saved}"
    assert saved["style"] == 4 and saved["invitesIgnored"] == 1, f"settings not saved: {saved}"
    print(f"preferences survived re-login: {saved}")

    # restore tester's defaults so the real client isn't left muted
    r = login("tester")
    r.send(2565, struct.pack(">iii", 100, 100, 100))
    r.send(3760, struct.pack(">i", 0))
    r.send(3271, bytes([0]))
    time.sleep(0.5)
    r.sock.close()

    print("ALL MULTIPLAYER CHECKS PASSED")


if __name__ == "__main__":
    main()
