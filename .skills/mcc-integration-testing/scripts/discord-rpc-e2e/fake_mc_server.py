#!/usr/bin/env python3
"""
Fake Minecraft Server for testing MCC.
Handles:
  - Server List Ping (status)
  - Offline-mode Login (no encryption)
  - Completes login phase but does NOT send JoinGame
  - Sends keep-alive packets to maintain connection
Uses protocol 763 (Minecraft 1.20.1) to avoid the Configuration phase.

The key insight: MCC's DiscordRpc bot initializes during the login phase,
before JoinGame is processed. We keep the connection open so the async
RPC send can complete without needing complex NBT registry data.
"""
import json
import os
import socket
import struct
import sys
import time
import threading
import uuid

HOST = "127.0.0.1"
PORT = 25565
PROTOCOL_VERSION = 763  # 1.20.1
MC_VERSION = "1.20.1"
LOG_FILE = "/tmp/e2e-test/fake_mc_server.log"

client_joined = threading.Event()


def log(msg):
    timestamp = time.strftime("%H:%M:%S")
    line = f"[{timestamp}] [FakeMCServer] {msg}"
    print(line, flush=True)
    with open(LOG_FILE, "a") as f:
        f.write(line + "\n")


# -- VarInt encoding/decoding --

def write_varint(value):
    """Encode a VarInt."""
    result = bytearray()
    value &= 0xFFFFFFFF  # handle negative values
    while True:
        byte = value & 0x7F
        value >>= 7
        if value != 0:
            byte |= 0x80
        result.append(byte)
        if value == 0:
            break
    return bytes(result)


def read_varint(data, offset=0):
    """Decode a VarInt, return (value, new_offset)."""
    result = 0
    shift = 0
    while True:
        byte = data[offset]
        offset += 1
        result |= (byte & 0x7F) << shift
        if (byte & 0x80) == 0:
            break
        shift += 7
        if shift >= 35:
            raise ValueError("VarInt too large")
    # Sign extension for negative values
    if result & (1 << 31):
        result -= 1 << 32
    return result, offset


def read_varint_from_socket(sock):
    """Read a VarInt from socket one byte at a time."""
    result = 0
    shift = 0
    while True:
        byte_data = sock.recv(1)
        if not byte_data:
            raise ConnectionError("Connection closed")
        byte = byte_data[0]
        result |= (byte & 0x7F) << shift
        if (byte & 0x80) == 0:
            break
        shift += 7
        if shift >= 35:
            raise ValueError("VarInt too large")
    if result & (1 << 31):
        result -= 1 << 32
    return result


def write_string(s):
    """Encode a Minecraft string (VarInt length + UTF-8 bytes)."""
    encoded = s.encode("utf-8")
    return write_varint(len(encoded)) + encoded


def read_string(data, offset):
    """Decode a Minecraft string."""
    length, offset = read_varint(data, offset)
    s = data[offset:offset + length].decode("utf-8")
    return s, offset + length


def make_packet(packet_id, payload=b""):
    """Wrap data into a Minecraft packet: [length VarInt][packet_id VarInt][payload]."""
    pid = write_varint(packet_id)
    packet_data = pid + payload
    return write_varint(len(packet_data)) + packet_data


def read_packet(sock):
    """Read a full Minecraft packet from socket. Returns (packet_id, payload_bytes)."""
    length = read_varint_from_socket(sock)
    if length <= 0:
        return None, b""
    data = b""
    while len(data) < length:
        chunk = sock.recv(length - len(data))
        if not chunk:
            raise ConnectionError("Connection closed mid-packet")
        data += chunk
    packet_id, offset = read_varint(data, 0)
    return packet_id, data[offset:]


# -- Packet builders --

def build_status_response():
    """Build Status Response (0x00) packet."""
    status = {
        "version": {"name": MC_VERSION, "protocol": PROTOCOL_VERSION},
        "players": {"max": 20, "online": 1, "sample": []},
        "description": {"text": "MCC Discord RPC Test Server"},
        "enforcesSecureChat": False,
        "previewsChat": False
    }
    return make_packet(0x00, write_string(json.dumps(status)))


def build_ping_response(payload_bytes):
    """Build Ping Response (0x01) packet."""
    return make_packet(0x01, payload_bytes)


def build_login_success(username):
    """Build Login Success (0x02) packet for 1.20.1."""
    player_uuid = uuid.uuid3(uuid.NAMESPACE_DNS, f"OfflinePlayer:{username}")
    uuid_bytes = player_uuid.bytes
    name_bytes = write_string(username)
    num_properties = write_varint(0)  # No properties
    return make_packet(0x02, uuid_bytes + name_bytes + num_properties)


def build_keep_alive(keep_alive_id=0):
    """Build Keep Alive (0x24 for 1.20.1) packet."""
    return make_packet(0x24, struct.pack(">q", keep_alive_id))


def handle_client(conn, addr):
    """Handle a single MCC client connection."""
    log(f"Client connected from {addr}")
    state = "handshake"  # handshake -> status or login -> play
    username = None

    try:
        while True:
            packet_id, payload = read_packet(conn)
            if packet_id is None:
                break

            if state == "handshake":
                if packet_id == 0x00:
                    # Handshake packet
                    proto, off = read_varint(payload, 0)
                    host, off = read_string(payload, off)
                    port = struct.unpack(">H", payload[off:off+2])[0]
                    off += 2
                    next_state, off = read_varint(payload, off)
                    log(f"Handshake: protocol={proto}, host={host}, port={port}, next_state={next_state}")

                    if next_state == 1:
                        state = "status"
                    elif next_state == 2:
                        state = "login"

            elif state == "status":
                if packet_id == 0x00:
                    # Status Request
                    log("Status Request received, sending response")
                    conn.sendall(build_status_response())
                elif packet_id == 0x01:
                    # Ping
                    log("Ping received, sending Pong")
                    conn.sendall(build_ping_response(payload))
                    break  # Status connection is done

            elif state == "login":
                if packet_id == 0x00:
                    # Login Start
                    username, off = read_string(payload, 0)
                    log(f"Login Start: username={username}")

                    # Send Login Success (offline mode - no encryption)
                    log(f"Sending Login Success for {username}")
                    conn.sendall(build_login_success(username))

                    state = "play"

                    # Signal that client joined (login phase complete)
                    client_joined.set()
                    log(f"*** {username} has logged in successfully! ***")

                    # Keep the connection alive without sending JoinGame
                    # MCC's DiscordRpc bot initializes during the login phase,
                    # before JoinGame is processed. We keep the connection open
                    # so the async RPC send can complete.
                    log("Keeping connection alive (not sending JoinGame to avoid NBT complexity)...")

                    # Start keep-alive loop
                    ka_thread = threading.Thread(
                        target=keep_alive_loop, args=(conn,), daemon=True
                    )
                    ka_thread.start()

            elif state == "play":
                # Just absorb play-state packets from the client silently
                pass

    except (ConnectionError, ConnectionResetError, BrokenPipeError) as e:
        log(f"Client disconnected: {e}")
    except Exception as e:
        log(f"Error: {e}")
    finally:
        conn.close()
        if username:
            log(f"{username} disconnected")


def keep_alive_loop(conn):
    """Send keep-alive packets every 10 seconds."""
    ka_id = 0
    try:
        while True:
            time.sleep(10)
            ka_id += 1
            conn.sendall(build_keep_alive(ka_id))
    except Exception:
        pass


def main():
    with open(LOG_FILE, "w") as f:
        f.write("")

    sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    sock.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
    sock.bind((HOST, PORT))
    sock.listen(5)

    log(f"Fake MC Server listening on {HOST}:{PORT} (protocol {PROTOCOL_VERSION}, {MC_VERSION})")
    log("Waiting for MCC connections...")

    try:
        while True:
            conn, addr = sock.accept()
            t = threading.Thread(target=handle_client, args=(conn, addr), daemon=True)
            t.start()
    except KeyboardInterrupt:
        log("Shutting down...")
    finally:
        sock.close()


if __name__ == "__main__":
    main()
