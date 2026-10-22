"""Logs in and sends packets, printing every packet the server answers with, by name.

    python tools/packet_probe.py <username> "<Name> [field ...]" ...

Each packet is a ClientMessages name followed by its fields: i:<int>, s:<text>, b:<0|1>, y:<byte>.
The server's replies are printed for a moment after each send, with their raw payload in hex.
Write a space inside a string as %20 and a line break as %0A. A "wait:<seconds>" step keeps listening that long, to hold a session open alongside another.

    python tools/packet_probe.py tester "GetBadgesEvent" "SetActivatedBadgesEvent i:1 s:ADM i:2 s: i:3 s: i:4 s: i:5 s:"
"""
import os, re, socket, struct, sys, time

sys.path.insert(0, os.path.dirname(__file__))
from protocol_probe import connect_and_login, wstr  # noqa: E402

ROOT = os.path.join(os.path.dirname(__file__), "..", "src", "Habbo.Protocol")


def headers(file):
    text = open(os.path.join(ROOT, file), encoding="utf-8-sig").read()
    return {name: int(value) for name, value in re.findall(r"const ushort (\w+) = (\d+);", text)}


CLIENT = headers("ClientMessages.cs")
SERVER = {value: name for name, value in headers("ServerMessages.cs").items()}


def encode(fields):
    out = b""
    for field in fields:
        kind, _, value = field.partition(":")
        if kind == "i":
            out += struct.pack(">i", int(value))
        elif kind == "s":
            out += wstr(value.replace("%20", " ").replace("%0A", "\n"))
        elif kind == "b":
            out += bytes([1 if value in ("1", "true") else 0])
        elif kind == "y":
            out += bytes([int(value) & 0xFF])
        elif kind == "h":
            out += struct.pack(">h", int(value))
        else:
            raise SystemExit(f"unknown field {field!r}")
    return out


def drain(conn, seconds):
    conn.sock.settimeout(0.2)
    end = time.time() + seconds
    while time.time() < end:
        try:
            message = conn.recv()
        except socket.timeout:
            continue
        header = message.u16()
        payload = message.data[message.o:]
        print(f"  <- {header} {SERVER.get(header, '?')} {payload[:120].hex()}")


def main():
    user, packets = sys.argv[1], sys.argv[2:]
    conn = connect_and_login(user)
    drain(conn, 1.5)
    for packet in packets:
        if packet.startswith("wait:"):
            drain(conn, float(packet[5:]))
            continue
        name, *fields = packet.split(" ")
        print(f"-> {name} {' '.join(fields)}")
        conn.send(CLIENT[name], encode(fields))
        drain(conn, 1.0)


if __name__ == "__main__":
    main()
