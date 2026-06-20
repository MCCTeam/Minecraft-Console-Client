#!/bin/bash
# Send an RCON command to a Minecraft server
# Usage: mc-rcon.sh <command> [port] [password]
set -euo pipefail

CMD="${1:?Usage: mc-rcon.sh <command> [port] [password]}"
PORT="${2:-25575}"
PW="${3:-test123}"

python3 -c "
import socket, struct, sys

s = socket.socket()
s.settimeout(5)
try:
    s.connect(('localhost', $PORT))
except Exception as e:
    print(f'Connection failed: {e}', file=sys.stderr)
    sys.exit(1)

def send(req_id, pkt_type, body):
    body = body.encode()
    s.send(struct.pack('<iii', 10 + len(body), req_id, pkt_type) + body + b'\x00\x00')

def recv():
    length = struct.unpack('<i', s.recv(4))[0]
    data = b''
    while len(data) < length:
        data += s.recv(length - len(data))
    return data

send(1, 3, '$PW')
r = recv()
rid = struct.unpack('<i', r[:4])[0]
if rid == -1:
    print('Auth failed', file=sys.stderr)
    s.close()
    sys.exit(1)

send(2, 2, \"\"\"$CMD\"\"\")
r = recv()
body = r[8:-2].decode(errors='replace')
if body:
    print(body)
s.close()
"
