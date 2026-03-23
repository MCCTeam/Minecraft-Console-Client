#!/usr/bin/env python3
"""
Fake Discord IPC server.
Listens on a Unix domain socket at the Discord IPC path and handles
the discord-rpc-csharp handshake + SET_ACTIVITY commands.
Logs all received presence updates to stdout and a log file.
"""
import json
import os
import socket
import struct
import sys
import time
import threading

# Discord IPC opcodes
OP_HANDSHAKE = 0
OP_FRAME = 1
OP_CLOSE = 2
OP_PING = 3
OP_PONG = 4

LOG_FILE = "/tmp/e2e-test/discord_rpc.log"
SOCKET_PATH = None

presence_received = threading.Event()
all_presences = []


def log(msg):
    timestamp = time.strftime("%H:%M:%S")
    line = f"[{timestamp}] [FakeDiscordIPC] {msg}"
    print(line, flush=True)
    with open(LOG_FILE, "a") as f:
        f.write(line + "\n")


def get_socket_path():
    """Determine where discord-rpc-csharp will look for the IPC socket."""
    runtime_dir = os.environ.get("XDG_RUNTIME_DIR")
    if runtime_dir:
        return os.path.join(runtime_dir, "discord-ipc-0")

    tmpdir = os.environ.get("TMPDIR", "/tmp")
    return os.path.join(tmpdir, "discord-ipc-0")


def read_message(conn):
    """Read a Discord IPC message: 4-byte LE opcode + 4-byte LE length + JSON."""
    header = b""
    while len(header) < 8:
        chunk = conn.recv(8 - len(header))
        if not chunk:
            return None, None
        header += chunk

    opcode, length = struct.unpack("<II", header)

    data = b""
    while len(data) < length:
        chunk = conn.recv(length - len(data))
        if not chunk:
            return None, None
        data += chunk

    try:
        payload = json.loads(data.decode("utf-8"))
    except (json.JSONDecodeError, UnicodeDecodeError):
        payload = data

    return opcode, payload


def send_message(conn, opcode, payload):
    """Send a Discord IPC message."""
    data = json.dumps(payload).encode("utf-8")
    header = struct.pack("<II", opcode, len(data))
    conn.sendall(header + data)


def handle_client(conn, addr):
    """Handle a single Discord RPC client connection."""
    log(f"Client connected: {addr}")

    try:
        while True:
            opcode, payload = read_message(conn)
            if opcode is None:
                log("Client disconnected")
                break

            if opcode == OP_HANDSHAKE:
                client_id = payload.get("client_id", "unknown")
                log(f"HANDSHAKE received - client_id: {client_id}, version: {payload.get('v')}")

                # Send READY response
                ready_response = {
                    "cmd": "DISPATCH",
                    "evt": "READY",
                    "data": {
                        "v": 1,
                        "config": {
                            "cdn_host": "cdn.discordapp.com",
                            "api_endpoint": "https://discord.com/api",
                            "environment": "production"
                        },
                        "user": {
                            "id": "123456789012345678",
                            "username": "TestUser",
                            "discriminator": "0",
                            "avatar": None,
                            "global_name": "TestUser"
                        }
                    },
                    "nonce": None
                }
                send_message(conn, OP_FRAME, ready_response)
                log("Sent READY response (user: TestUser)")

            elif opcode == OP_FRAME:
                cmd = payload.get("cmd", "UNKNOWN")
                nonce = payload.get("nonce")

                if cmd == "SET_ACTIVITY":
                    activity = payload.get("args", {}).get("activity", {})
                    log(f"*** SET_ACTIVITY received ***")
                    log(f"  Details : {activity.get('details', 'N/A')}")
                    log(f"  State   : {activity.get('state', 'N/A')}")

                    assets = activity.get("assets", {})
                    if assets:
                        log(f"  Large   : {assets.get('large_image', 'N/A')} ({assets.get('large_text', '')})")
                        log(f"  Small   : {assets.get('small_image', 'N/A')} ({assets.get('small_text', '')})")

                    timestamps = activity.get("timestamps", {})
                    if timestamps:
                        log(f"  Start   : {timestamps.get('start', 'N/A')}")

                    party = activity.get("party", {})
                    if party:
                        log(f"  Party   : {party.get('id', '')} size={party.get('size', [])}")

                    all_presences.append(activity)
                    presence_received.set()

                    # Send success response
                    response = {
                        "cmd": "SET_ACTIVITY",
                        "evt": None,
                        "data": activity,
                        "nonce": nonce
                    }
                    send_message(conn, OP_FRAME, response)
                    log("Sent SET_ACTIVITY acknowledgement")

                else:
                    log(f"FRAME received - cmd: {cmd}, payload: {json.dumps(payload, indent=2)[:200]}")
                    # Generic ack
                    response = {
                        "cmd": cmd,
                        "evt": None,
                        "data": {},
                        "nonce": nonce
                    }
                    send_message(conn, OP_FRAME, response)

            elif opcode == OP_CLOSE:
                log(f"CLOSE received: {payload}")
                break

            elif opcode == OP_PING:
                log("PING received, sending PONG")
                send_message(conn, OP_PONG, payload)

            else:
                log(f"Unknown opcode {opcode}: {payload}")

    except Exception as e:
        log(f"Error handling client: {e}")
    finally:
        conn.close()
        log("Client connection closed")


def main():
    global SOCKET_PATH

    SOCKET_PATH = get_socket_path()

    # Clean up old socket
    if os.path.exists(SOCKET_PATH):
        os.unlink(SOCKET_PATH)

    # Clear log
    with open(LOG_FILE, "w") as f:
        f.write("")

    sock = socket.socket(socket.AF_UNIX, socket.SOCK_STREAM)
    sock.bind(SOCKET_PATH)
    sock.listen(5)

    # Ensure the socket is world-readable/writable
    os.chmod(SOCKET_PATH, 0o777)

    log(f"Fake Discord IPC listening on: {SOCKET_PATH}")
    log("Waiting for RPC client connections...")

    try:
        while True:
            conn, addr = sock.accept()
            t = threading.Thread(target=handle_client, args=(conn, addr), daemon=True)
            t.start()
    except KeyboardInterrupt:
        log("Shutting down...")
    finally:
        sock.close()
        if os.path.exists(SOCKET_PATH):
            os.unlink(SOCKET_PATH)


if __name__ == "__main__":
    main()
