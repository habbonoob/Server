"""Plays a full WIN63 client session against the emulator: handshake, login, navigator,
room entry, walking, chat. Verifies server responses at every step."""
import socket, struct, random, sys, time, json, urllib.request

HOST, PORT, WEB = "127.0.0.1", 30000, "http://localhost:8080"

N = int("86851dd364d5c5cece3c883171cc6ddc5760779b992482bd1e20dd296888df91b33b936a7b93f06d29e8870f703a216257dec7c81de0058fea4cc5116f75e6efc4e9113513e45357dc3fd43d4efab5963ef178b78bd61e81a14c603b24c8bcce0a12230b320045498edc29282ff0603bc7b7dae8fc1b05b52b2f301a9dc783b7", 16)
E = 3

# the keepalive: the server pings, the client answers
PING = 35
PONG = 1824

class Rc4:
    def __init__(self, key):
        self.s = list(range(256)); j = 0
        for i in range(256):
            j = (j + self.s[i] + key[i % len(key)]) & 0xFF
            self.s[i], self.s[j] = self.s[j], self.s[i]
        self.i = self.j = 0
    def process(self, data):
        out = bytearray()
        for b in data:
            self.i = (self.i + 1) & 0xFF
            self.j = (self.j + self.s[self.i]) & 0xFF
            self.s[self.i], self.s[self.j] = self.s[self.j], self.s[self.i]
            out.append(b ^ self.s[(self.s[self.i] + self.s[self.j]) & 0xFF])
        return bytes(out)
    def snapshot(self):
        return self.s[:], self.i, self.j
    def restore(self, snap):
        self.s, self.i, self.j = snap

def rsa_public_op(x):  # x^e mod n - recovers type-1 signatures and encrypts type-2 blocks
    return pow(x, E, N)

def recover_type1(sig):  # client-side "verify": sig^e mod n, strip 00 01 FF.. 00
    block = rsa_public_op(int.from_bytes(sig, "big")).to_bytes(128, "big")
    assert block[0] == 0 and block[1] == 1 and block[2] == 0xFF
    sep = block.index(0, 2)
    return block[sep + 1:]

def encrypt_type2(data):  # PKCS#1 v1.5 type 2 with e=3
    block = bytearray(128); block[1] = 2
    ps = bytes(random.Random(42).randbytes(128 - 3 - len(data)))  # nonzero-ish padding
    block[2:2+len(ps)] = ps
    block[128 - len(data) - 1] = 0
    block[128 - len(data):] = data
    return pow(int.from_bytes(block, "big"), E, N).to_bytes(128, "big")

def wstr(s):
    b = s.encode()
    return struct.pack(">H", len(b)) + b

def frame(header, payload=b"", cipher=None):
    body = struct.pack(">H", header) + payload
    raw = struct.pack(">i", len(body)) + body
    return cipher.process(raw) if cipher else raw

class Msg:
    def __init__(self, data, offset=0):
        self.data, self.o = data, offset
    def u8(self): v = self.data[self.o]; self.o += 1; return v
    def u16(self): v = struct.unpack(">H", self.data[self.o:self.o+2])[0]; self.o += 2; return v
    def i32(self): v = struct.unpack(">i", self.data[self.o:self.o+4])[0]; self.o += 4; return v
    def s(self):
        n = self.u16(); v = self.data[self.o:self.o+n].decode(); self.o += n; return v

class Conn:
    def __init__(self):
        self.sock = socket.create_connection((HOST, PORT), timeout=10)
        self.buf = b""
        self.queue = []
        self.rc4_in = self.rc4_out = None
    def send(self, header, payload=b"", plain=False):
        self.sock.sendall(frame(header, payload, None if plain else self.rc4_out))
    def recv(self):
        while not self.queue:
            self.queue.extend(self.frames())
            if not self.queue:
                self.buf += self.sock.recv(65536)
        return self.queue.pop(0)
    def frames(self):
        out = []
        while True:
            if len(self.buf) < 6: break
            # Like the real client: mark/reset around the length read so the cipher
            # only advances when a whole frame has been consumed.
            snap = self.rc4_in.snapshot() if self.rc4_in else None
            ln_bytes = self.rc4_in.process(self.buf[:4]) if self.rc4_in else self.buf[:4]
            ln = int.from_bytes(ln_bytes, "big")
            if ln < 2 or ln > 262144: raise Exception(f"bad length {ln}")
            if len(self.buf) < 4 + ln:
                if snap: self.rc4_in.restore(snap)
                break
            body = self.buf[4:4+ln]
            if self.rc4_in: body = self.rc4_in.process(body)
            self.buf = self.buf[4+ln:]
            out.append(Msg(body))
        return out

def drain_until(conn, header, timeout=6.0):
    """Reads until the wanted header arrives, skipping anything else the server pushes."""
    conn.sock.settimeout(timeout)
    while True:
        message = conn.recv()
        if message.u16() == header:
            return message


def connect_and_login(username, password="password"):
    """Full handshake plus SSO login, returning a Conn ready for game messages.
    Shared with the other probes so the sequence lives in one place."""
    request = urllib.request.Request(
        WEB + "/api/auth/login",
        data=json.dumps({"username": username, "password": password}).encode(),
        headers={"Content-Type": "application/json"})
    ticket = json.load(urllib.request.urlopen(request))["ssoTicket"]

    c = Conn()
    c.send(4000, wstr("WIN63-202603212315-320263584") + wstr("FLASH25") + struct.pack(">ii", 6, 4), plain=True)
    c.send(2735, plain=True)

    m = c.recv()
    assert m.u16() == 1142, "expected the DH prime/generator"
    prime = int(recover_type1(bytes.fromhex(m.s())))
    generator = int(recover_type1(bytes.fromhex(m.s())))

    secret = random.getrandbits(240)
    c.send(764, wstr(encrypt_type2(str(pow(generator, secret, prime)).encode()).hex()), plain=True)

    m = c.recv()
    assert m.u16() == 1038, "expected the server's DH public key"
    shared = pow(int(recover_type1(bytes.fromhex(m.s()))), secret, prime)
    key_hex = ("%x" % shared).upper()
    if len(key_hex) % 2:
        key_hex = "0" + key_hex  # the client pads odd-length hex with a leading nibble
    key = bytes.fromhex(key_hex)
    c.rc4_in, c.rc4_out = Rc4(key), Rc4(key)

    c.send(1861, struct.pack(">i", 401) + wstr("app:/") + wstr("x"))
    c.send(1951, wstr("machine") + wstr("fp") + wstr("WIN 51,2,1,1"))
    c.send(2354, wstr(ticket) + struct.pack(">i", 0))

    # the keepalive may ping the connection before it is logged in; answer and read on
    while True:
        m = c.recv()
        header = m.u16()
        if header != PING:
            break
        c.send(PONG)
    assert header == 3847, f"expected AuthenticationOK, got {header}"
    return c


def main():
    # --- login over HTTP
    req = urllib.request.Request(WEB + "/api/auth/login", data=json.dumps(
        {"username": "admin", "password": "password"}).encode(), headers={"Content-Type": "application/json"})
    ticket = json.load(urllib.request.urlopen(req))["ssoTicket"]
    print("ticket:", ticket)

    c = Conn()
    c.send(4000, wstr("WIN63-202603212315-320263584") + wstr("FLASH25") + struct.pack(">ii", 6, 4), plain=True)
    c.send(2735, plain=True)

    m = c.recv(); assert m.u16() == 1142, f"expected 1142 got {m.o and '?'}"
    prime = int(recover_type1(bytes.fromhex(m.s())))
    gen = int(recover_type1(bytes.fromhex(m.s())))
    print(f"dh prime={prime} gen={gen}")

    a = random.getrandbits(240)
    pub = pow(gen, a, prime)
    c.send(764, wstr(encrypt_type2(str(pub).encode()).hex()), plain=True)

    m = c.recv(); assert m.u16() == 1038
    server_pub = int(recover_type1(bytes.fromhex(m.s())))
    encrypted = m.u8() == 1
    assert encrypted
    shared = pow(server_pub, a, prime)
    key_hex = ("%x" % shared).upper()
    if len(key_hex) % 2:
        key_hex = "0" + key_hex  # client: odd-length hex gets a leading "0" nibble
    key = bytes.fromhex(key_hex)
    c.rc4_in = Rc4(key); c.rc4_out = Rc4(key)
    print("handshake complete, rc4 on")

    c.send(1861, struct.pack(">i", 401) + wstr("app:/") + wstr("x"))
    c.send(1951, wstr("machine") + wstr("fp") + wstr("WIN 51,2,1,1"))
    c.send(2354, wstr(ticket) + struct.pack(">i", 0))

    m = c.recv(); assert m.u16() == 3847, f"expected AuthenticationOK got {m.u16()}"
    account = m.i32(); count = m.i32(); identity = m.i32()
    print(f"authenticated: account={account} actions={count} identity={identity}")

    c.send(1484)
    got = {}
    for _ in range(7):
        m = c.recv(); got[m.u16()] = m
    assert 1338 in got and 2539 in got and 1287 in got, f"hotel view missing: {got.keys()}"
    assert 3291 in got, "PerkAllowances missing - the client's chat would crash without it"
    assert 2809 in got, "AccountPreferences missing - chat style would be undefined"
    print("hotel view packets:", list(got))

    # --- navigator
    c.send(627)
    m = drain_until(c, 3474)
    print("navigator metadata ok")

    c.send(815, wstr("my_rooms") + wstr(""))
    m = drain_until(c, 1893)  # init also pushes FavouritesEvent, so skip past it
    search_code, filtering, blocks = m.s(), m.s(), m.i32()
    block_code, text, allowed, closed, mode, rooms = m.s(), m.s(), m.i32(), m.u8(), m.i32(), m.i32()
    room_id = m.i32(); room_name = m.s()
    print(f"search '{search_code}' -> {rooms} room(s): #{room_id} '{room_name}'")

    # --- enter the room
    c.send(3176, struct.pack(">iii", room_id, 0, 1))
    m = c.recv(); assert m.u16() == 3576
    enter = m.u8() == 1
    print("getGuestRoom result: enterRoom=", enter)

    c.send(3463, struct.pack(">i", room_id) + wstr("") + struct.pack(">i", -1))
    seq = []
    m = c.recv(); assert m.u16() == 675, f"expected RoomReady got {m.u16()}"
    model_name, rid = m.s(), m.i32()
    m = c.recv(); assert m.u16() == 547
    m = c.recv(); assert m.u16() == 982, f"expected HeightMap got {m.u16()}"
    m = c.recv(); assert m.u16() == 1580, f"expected FloorHeightMap got {m.u16()}"
    small, walls, heightmap = m.u8(), m.i32(), m.s()
    print(f"room ready: model={model_name} heightmap rows={len(heightmap.split(chr(13)))}")
    m = c.recv(); assert m.u16() == 709  # floor
    m = c.recv(); assert m.u16() == 709  # wallpaper
    m = c.recv(); assert m.u16() == 709  # landscape
    m = c.recv(); assert m.u16() == 1145
    m = c.recv(); assert m.u16() == 1818
    m = c.recv(); assert m.u16() == 437
    m = c.recv(); assert m.u16() == 2519, f"expected Users got {m.u16()}"
    user_count = m.i32()
    web_id = m.i32(); name = m.s(); custom = m.s(); figure = m.s()
    index = m.i32(); x = m.i32(); y = m.i32(); z = m.s(); d = m.i32(); utype = m.i32()
    print(f"users packet: count={user_count} webID={web_id} name={name} index={index} pos=({x},{y},{z})")
    assert web_id == account, "webID must equal login UserObject id!"
    m = c.recv(); assert m.u16() == 3657, f"expected everyone's postures (UserUpdate) got {m.u16()}"
    m = c.recv(); assert m.u16() == 3001, f"expected RoomEntryInfo got {m.u16()}"
    entry_room, is_owner = m.i32(), m.u8() == 1
    print(f"room entry info: room={entry_room} owner={is_owner}")
    m = c.recv(); assert m.u16() == 2540, f"expected YouAreOwner got {m.u16()}"    # YouAreOwner
    m = c.recv(); assert m.u16() == 2587, f"expected YouAreController got {m.u16()}"  # YouAreController
    c.send(3176, struct.pack(">iii", room_id, 1, 0))
    m = c.recv(); assert m.u16() == 3576, f"expected GetGuestRoomResult got {m.u16()}"
    print("in room!")

    # --- walk to (7, 1)
    c.send(2256, struct.pack(">ii", 7, 1))
    moves = []
    deadline = time.time() + 5
    while time.time() < deadline and len(moves) < 8:
        c.sock.settimeout(0.5)
        try:
            m = c.recv()
        except socket.timeout:
            continue
        if m.u16() != 3657:
            print("unexpected while walking:", m.u16()); continue
        count = m.i32()
        for _ in range(count):
            eid, ex, ey, ez, head, body, status = m.i32(), m.i32(), m.i32(), m.s(), m.i32(), m.i32(), m.s()
            moves.append((ex, ey, status))
    print("user updates:", moves)
    # Semantics: position = the tile being LEFT, mv = the tile being entered.
    # A zero-delta update (position == mv) would teleport instead of slide.
    assert moves[0][0:2] == (3, 1), f"first update position must be the start tile, got {moves[0]}"
    mv_targets = []
    for ex, ey, status in moves:
        if status.startswith("mv "):
            tx, ty = [int(v) for v in status[3:].split(",")[:2]]
            mv_targets.append((tx, ty))
    for i in range(len(mv_targets) - 1):
        assert mv_targets[i] == moves[i + 1][0:2],             f"step {i}: mv target {mv_targets[i]} != next position {moves[i+1][0:2]}"
    assert len(mv_targets) >= 3, "expected a multi-tile walk"
    assert moves[-1][2] == "" and moves[-1][0:2] == (7, 1), f"walk did not end at destination: {moves[-1]}"

    # --- redirect mid-walk: start walking right, then click back toward the left
    def collect(duration):
        out = []
        deadline = time.time() + duration
        while time.time() < deadline:
            c.sock.settimeout(0.4)
            try:
                m = c.recv()
            except socket.timeout:
                continue
            if m.u16() != 3657:
                continue
            count = m.i32()
            for _ in range(count):
                eid, ex, ey, ez, head, body, status = m.i32(), m.i32(), m.i32(), m.s(), m.i32(), m.i32(), m.s()
                out.append((ex, ey, status))
        return out

    c.send(2256, struct.pack(">ii", 10, 1))
    time.sleep(1.0)  # two steps in
    c.send(2256, struct.pack(">ii", 5, 1))  # redirect back to the left
    updates = collect(4.0)
    assert updates and updates[-1][2] == "" and updates[-1][0:2] == (5, 1),         f"redirect did not end at (5,1): {updates[-3:]}"
    print("redirect ok:", updates[-3:])

    # --- clicking the tile being entered must NOT interrupt the walk (a stand update
    #     mid-slide would snap the animation � real Habbo treats this as a no-op)
    c.send(2256, struct.pack(">ii", 10, 1))
    c.sock.settimeout(2.0)
    m = c.recv()
    assert m.u16() == 3657
    assert m.i32() == 1  # batched count
    eid, ex, ey, ez, head, body, status = m.i32(), m.i32(), m.i32(), m.s(), m.i32(), m.i32(), m.s()
    assert status.startswith("mv "), f"expected a step, got '{status}'"
    entering = tuple(int(v) for v in status[3:].split(",")[:2])
    c.send(2256, struct.pack(">ii", entering[0], entering[1]))  # click the next step -> no-op
    updates = collect(4.0)
    assert updates and updates[-1][2] == "" and updates[-1][0:2] == (10, 1),         f"walk interrupted after no-op click: {updates[-3:]}"
    assert not any(u[2] == "" for u in updates[:-1]), f"stand update mid-walk: {updates[:3]}"
    print("no-op click ok: walk continued to (10,1)")

    # --- re-entry: the client re-entering a room must get the full sequence again
    c.send(3463, struct.pack(">i", room_id) + wstr("") + struct.pack(">i", -1))
    m = c.recv(); assert m.u16() == 675, f"re-entry: expected RoomReady got {m.u16()}"
    model_name, rid = m.s(), m.i32()
    m = c.recv(); assert m.u16() == 547
    m = c.recv(); assert m.u16() == 982
    m = c.recv(); assert m.u16() == 1580, "re-entry: FloorHeightMap missing"
    for _ in range(10):  # 3x RoomProperty, 1145, 1818, 437, 2519, 3657, 3001, 2540 (2587 below)
        m = c.recv()
    m = c.recv(); assert m.u16() == 2587
    print("re-entry ok: full sequence received again")

    # --- inventory
    c.send(3765)
    m = c.recv(); assert m.u16() == 3652, f"expected FurniList got {m.u16()}"
    total_fragments, fragment_no, item_count = m.i32(), m.i32(), m.i32()
    first_item_id = None
    first_chair_id = None
    for _ in range(item_count):
        item_id, item_type, room_item_id, class_id, category = m.i32(), m.s(), m.i32(), m.i32(), m.i32()
        m.i32(); m.s()                      # StuffData: type + legacy string
        m.u8(); m.u8(); m.u8(); m.u8()      # recyclable, tradeable, groupable, sellable
        m.i32(); m.u8(); m.i32()            # secondsToExpiration, hasRentPeriodStarted, flatId
        if item_type == "S":
            m.s(); m.i32()                  # slotId, extra
        if first_item_id is None:
            first_item_id = item_id
        if first_chair_id is None and class_id == 26:   # chair_silo
            first_chair_id = item_id
    assert item_count > 0, "inventory is empty"
    assert first_chair_id is not None, "no chair in the seeded inventory"
    print(f"inventory: {item_count} item(s), chair strip id {first_chair_id}")

    # --- place the chair at (6,3) facing 2
    c.send(1743, wstr(f"{first_chair_id} 6 3 2"))
    m = c.recv(); assert m.u16() == 3228, f"expected ObjectAdd got {m.u16()}"
    obj_id, obj_class, obj_x, obj_y, obj_dir = m.i32(), m.i32(), m.i32(), m.i32(), m.i32()
    obj_z, obj_sizez = m.s(), m.s()
    assert (obj_x, obj_y) == (6, 3) and obj_class == 26, f"chair landed wrong: {obj_x},{obj_y} class {obj_class}"
    print(f"placed chair: id={obj_id} at ({obj_x},{obj_y}) dir={obj_dir} z={obj_z} sizeZ={obj_sizez}")
    m = c.recv(); assert m.u16() == 3440, "expected HeightMapUpdate after placement"
    tile_count = m.i32()
    assert tile_count >= 1
    m = c.recv(); assert m.u16() == 1372, "expected FurniListRemove after placement"

    # --- rotate it (MoveObject with a new direction)
    c.send(3018, struct.pack(">iiii", obj_id, 6, 3, 4))
    m = c.recv(); assert m.u16() == 3025, f"expected ObjectUpdate got {m.u16()}"
    m.i32(); m.i32(); m.i32(); m.i32()
    new_dir = m.i32()
    assert new_dir == 4, f"rotation not applied: {new_dir}"
    print("rotated chair to dir 4")
    m = c.recv(); assert m.u16() == 3440

    # --- walk onto the chair: the server must apply the sit on arrival
    c.send(2256, struct.pack(">ii", 6, 3))
    sat = None
    deadline = time.time() + 6
    while time.time() < deadline and sat is None:
        c.sock.settimeout(0.5)
        try:
            m = c.recv()
        except socket.timeout:
            continue
        if m.u16() != 3657:
            continue
        for _ in range(m.i32()):
            eid, ex, ey, ez, head, body, status = m.i32(), m.i32(), m.i32(), m.s(), m.i32(), m.i32(), m.s()
            if status.startswith("sit "):
                sat = (ex, ey, status)
    assert sat is not None, "server never applied the sit"
    assert sat[0:2] == (6, 3), f"sat on the wrong tile: {sat}"
    assert sat[2].split()[2] == "1", f"canStandUp token missing: {sat[2]}"
    print(f"sat on chair: {sat[2]} at {sat[0]},{sat[1]}")

    # --- stand back up via the avatar menu
    c.send(2051, struct.pack(">i", 0))
    m = c.recv(); assert m.u16() == 3657
    m.i32()
    eid, ex, ey, ez, head, body, status = m.i32(), m.i32(), m.i32(), m.s(), m.i32(), m.i32(), m.s()
    assert status == "", f"stand did not clear the status: '{status}'"
    print("stood up")

    # --- pick the chair back up
    c.send(2125, struct.pack(">i", 2) + struct.pack(">i", obj_id) + bytes([0]))
    m = c.recv(); assert m.u16() == 1359, f"expected ObjectRemove got {m.u16()}"
    removed_id = int(m.s())
    assert removed_id == obj_id, f"removed the wrong item: {removed_id}"
    m = c.recv(); assert m.u16() == 3440, "expected HeightMapUpdate after pickup"
    m = c.recv(); assert m.u16() == 2151, f"expected FurniListAddOrUpdate got {m.u16()}"
    returned = m.i32()
    assert returned == 1
    back_id = m.i32()
    assert back_id == obj_id, "item did not return to the inventory"
    print(f"picked up chair {removed_id}, back in inventory")

    # --- chat: an emoticon must arrive as the matching avatar gesture
    c.send(1119, wstr("hello from the probe :)") + struct.pack(">ii", 0, 42))
    m = c.recv(); assert m.u16() == 947, f"expected chat echo got {m.u16()}"
    eid, text, gesture = m.i32(), m.s(), m.i32()
    assert gesture == 1, f"':)' should smile (gesture 1), got {gesture}"
    print(f"chat echoed to roomIndex {eid}: '{text}' with gesture {gesture}")

    # --- home room: setting it is persisted and confirmed via NavigatorSettings
    c.send(179, struct.pack(">i", room_id))
    m = c.recv(); assert m.u16() == 2539, f"expected NavigatorSettings got {m.u16()}"
    home, enter_room = m.i32(), m.i32()
    assert (home, enter_room) == (room_id, 0), f"home room not confirmed: {home}, {enter_room}"
    print(f"home room set to {home}")

    print("ALL PROTOCOL CHECKS PASSED")

if __name__ == "__main__":
    main()
