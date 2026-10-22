"""Repeats the full crypto handshake + SSO login many times. Key-derivation bugs are
data-dependent (they only fire for some shared secrets), so one successful login proves
little; hundreds do."""
import sys

from multiplayer_probe import login


def main(rounds):
    failures = 0
    for attempt in range(1, rounds + 1):
        try:
            c = login("admin")
            c.sock.close()
        except Exception as error:  # a desynced RC4 stream surfaces as a bad frame or timeout
            failures += 1
            print(f"login {attempt} failed: {error!r}")
        if attempt % 50 == 0:
            print(f"{attempt} logins, {failures} failure(s)")

    print(f"DONE: {rounds - failures}/{rounds} logins succeeded")
    sys.exit(1 if failures else 0)


if __name__ == "__main__":
    main(int(sys.argv[1]) if len(sys.argv) > 1 else 300)
