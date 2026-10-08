#!/usr/bin/env python3
"""
umpkcap.py — inspect UMPK .umpkcap capture files and MCC diagnostics bundles.

Format reference (all multi-byte LE):
  Header 23 bytes:
    0..5   magic "UMPKC1"
    6      version (1)
    7..14  recordedAtUnixMs int64
    15..18 protocol int32
    19..22 frameCount int32
  Each record 26 bytes prefix + body:
    0      direction  (0=clientbound, 1=serverbound)
    1      phase      (0=Handshake, 1=Status, 2=Login, 3=Configuration, 4=Play)
    2..9   sequence int64
    10..17 timestampTicks int64 (Stopwatch ticks, monotonic)
    18..21 wireId int32
    22..25 bodyLength int32
    26..   body bytes

Sources:
  - Umpk.Protocol.Java.Capture.UmpkCapFormat.cs (shipping protocol assembly)
  - Umpk.TestKit.Corpus.UmpkCapFormat.cs / CorpusLoader.cs (test kit, identical layout)

Usage:
  python3 tools/umpkcap.py <path>                 # path = .umpkcap, .zip, or directory
  python3 tools/umpkcap.py fixtures/corpus/477/login-config.umpkcap
  python3 tools/umpkcap.py /tmp/mcc-diagnostics.zip
  python3 tools/umpkcap.py /tmp/logs --json
  python3 tools/umpkcap.py capture.umpkcap --frame 42 --hex

  Options:
    --json          emit machine-readable JSON (one object per capture)
    --frame N       dump only frame N (0-based) in detail
    --hex           with --frame, also hex-dump body (default: try utf8 preview)
    --limit N       cap listing to first N frames (0 = all)
    --filter-phase NAME  only show frames of that phase (handshake,status,login,configuration,play)
    --filter-dir DIR     only show direction (clientbound,serverbound)

Diagnostics bundles (.zip) produced by Mcc.Cli/Diagnostics contain:
  server.json, client.json, system.json, console.log, packets.NNNN.umpkcap, crash.txt
This tool auto-discovers packets.*.umpkcap entries inside a zip without extracting.
"""
from __future__ import annotations

import argparse
import datetime
import json
import os
import struct
import sys
import textwrap
import zipfile
from dataclasses import dataclass
from pathlib import Path
from typing import Iterator, List, Optional

MAGIC = b"UMPKC1"
VERSION = 1
HEADER_LEN = 23
RECORD_PREFIX_LEN = 26

PHASE_NAMES = {
    0: "Handshake",
    1: "Status",
    2: "Login",
    3: "Configuration",
    4: "Play",
}
PHASE_BY_NAME = {v.lower(): k for k, v in PHASE_NAMES.items()}
DIR_NAMES = {
    0: "clientbound",
    1: "serverbound",
}
DIR_BY_NAME = {"clientbound": 0, "serverbound": 1, "c2s": 1, "s2c": 0, "client": 0, "server": 1}


@dataclass
class UmpkCapHeader:
    path: str
    recorded_at_ms: int
    recorded_at_iso: str
    protocol: int
    frame_count: int
    version: int

    @property
    def recorded_at_dt(self) -> Optional[datetime.datetime]:
        try:
            return datetime.datetime.fromtimestamp(self.recorded_at_ms / 1000, tz=datetime.timezone.utc)
        except Exception:
            return None


@dataclass
class UmpkCapFrame:
    sequence: int
    direction: int
    phase: int
    timestamp_ticks: int
    wire_id: int
    body: bytes

    @property
    def direction_name(self) -> str:
        return DIR_NAMES.get(self.direction, f"unknown({self.direction})")

    @property
    def phase_name(self) -> str:
        return PHASE_NAMES.get(self.phase, f"unknown({self.phase})")


@dataclass
class UmpkCapFile:
    header: UmpkCapHeader
    frames: List[UmpkCapFrame]
    raw_size: int
    truncated: bool = False
    error: Optional[str] = None


def parse_bytes(data: bytes, path: str = "<memory>") -> UmpkCapFile:
    if len(data) < HEADER_LEN:
        return UmpkCapFile(
            header=UmpkCapHeader(path, 0, "", 0, 0, 0),
            frames=[],
            raw_size=len(data),
            truncated=True,
            error=f"file too short ({len(data)} < {HEADER_LEN})",
        )
    magic = data[0:6]
    if magic != MAGIC:
        return UmpkCapFile(
            header=UmpkCapHeader(path, 0, "", 0, 0, 0),
            frames=[],
            raw_size=len(data),
            truncated=True,
            error=f"bad magic {magic!r} expected {MAGIC!r}",
        )
    version = data[6]
    if version != VERSION:
        return UmpkCapFile(
            header=UmpkCapHeader(path, 0, "", 0, 0, 0),
            frames=[],
            raw_size=len(data),
            truncated=True,
            error=f"unsupported version {version} expected {VERSION}",
        )
    recorded_at_ms = struct.unpack_from("<q", data, 7)[0]
    protocol = struct.unpack_from("<i", data, 15)[0]
    frame_count = struct.unpack_from("<i", data, 19)[0]
    try:
        iso = datetime.datetime.fromtimestamp(recorded_at_ms / 1000, tz=datetime.timezone.utc).isoformat()
    except Exception:
        iso = str(recorded_at_ms)

    header = UmpkCapHeader(
        path=path,
        recorded_at_ms=recorded_at_ms,
        recorded_at_iso=iso,
        protocol=protocol,
        frame_count=frame_count,
        version=version,
    )
    frames: List[UmpkCapFrame] = []
    offset = HEADER_LEN
    truncated = False
    error = None
    for i in range(frame_count):
        if offset + RECORD_PREFIX_LEN > len(data):
            truncated = True
            error = f"truncated record {i}: not enough bytes for prefix (offset {offset}, need {RECORD_PREFIX_LEN}, have {len(data)-offset})"
            break
        direction = data[offset]
        phase = data[offset + 1]
        sequence = struct.unpack_from("<q", data, offset + 2)[0]
        timestamp_ticks = struct.unpack_from("<q", data, offset + 10)[0]
        wire_id = struct.unpack_from("<i", data, offset + 18)[0]
        body_len = struct.unpack_from("<i", data, offset + 22)[0]
        offset += RECORD_PREFIX_LEN
        if body_len < 0:
            truncated = True
            error = f"record {i} negative body length {body_len}"
            break
        if offset + body_len > len(data):
            truncated = True
            error = f"record {i} body overruns file (need {body_len}, have {len(data)-offset})"
            break
        body = data[offset : offset + body_len]
        offset += body_len
        frames.append(UmpkCapFrame(sequence, direction, phase, timestamp_ticks, wire_id, body))

    # If file has trailing bytes beyond declared frames, note it (writer may have patched count lazily)
    if not truncated and offset != len(data):
        # Not fatal, but worth noting
        if error is None and len(data) - offset > 0:
            # Could be extra data; mark as truncated-like warning
            pass

    return UmpkCapFile(header=header, frames=frames, raw_size=len(data), truncated=truncated, error=error)


def parse_file(path: Path) -> UmpkCapFile:
    data = path.read_bytes()
    return parse_bytes(data, str(path))


def discover_inputs(raw_path: str) -> List[Path]:
    """Expand a path that may be a file, directory, or zip into concrete .umpkcap paths."""
    p = Path(raw_path)
    if not p.exists():
        return []
    if p.is_file() and p.suffix == ".zip":
        return [p]  # special-cased upstream
    if p.is_file() and p.suffix == ".umpkcap":
        return [p]
    if p.is_dir():
        return sorted(p.rglob("*.umpkcap"))
    # glob pattern?
    if "*" in raw_path or "?" in raw_path:
        import glob

        return sorted(Path(g) for g in glob.glob(raw_path, recursive=True) if Path(g).is_file())
    return []


def iter_captures_in_zip(zip_path: Path) -> Iterator[tuple[str, UmpkCapFile]]:
    with zipfile.ZipFile(zip_path, "r") as z:
        for info in z.infolist():
            if info.filename.endswith(".umpkcap"):
                data = z.read(info.filename)
                cap = parse_bytes(data, f"{zip_path}:{info.filename}")
                yield (info.filename, cap)


def describe_zip_without_captures(zip_path: Path) -> None:
    """Print a helpful summary when a diagnostics zip contains no captures (common when version resolution failed)."""
    with zipfile.ZipFile(zip_path, "r") as z:
        print(f"─ {zip_path} (diagnostics bundle, no .umpkcap entries)")
        print(f"  contents:")
        for info in z.infolist():
            print(f"    {info.file_size:7d}  {info.filename}")
        # Try to show console.log and server.json preview if present
        for name in ("console.log", "server.json", "client.json", "MANIFEST.md", "crash.txt"):
            if name in z.namelist():
                try:
                    data = z.read(name)
                    text = data.decode("utf-8", errors="replace")
                    preview = text.strip().splitlines()[:20]
                    print(f"\n  ─ {name} (first {len(preview)} lines):")
                    for line in preview:
                        print(f"    {line[:200]}")
                    if len(text.splitlines()) > 20:
                        print(f"    … ({len(text.splitlines())-20} more lines)")
                except Exception as e:
                    print(f"  (could not read {name}: {e})")
        print("\n  note: no packets.*.umpkcap — capture starts at first non-login frame.")
        print("  version-resolution failure happens *before* any capturable frame (status ping uses")
        print("  a separate short-lived connection in ServerVersionNegotiator.DetectAsync), so an")
        print("  early UnsupportedProtocol exit correctly yields an empty packet capture.")
        print("  Inspect console.log above for the 'Version: … (Protocol: -1)' line and the")
        print("  'Version resolution failed: protocol not in the supported catalog' error.")


def human_bytes(n: int) -> str:
    for unit in ["B", "KB", "MB", "GB"]:
        if abs(n) < 1024 or unit == "GB":
            return f"{n:.1f}{unit}" if unit != "B" else f"{n}{unit}"
        n /= 1024
    return str(n)


def hexdump(data: bytes, width: int = 16) -> str:
    lines = []
    for i in range(0, len(data), width):
        chunk = data[i : i + width]
        hexs = " ".join(f"{b:02x}" for b in chunk)
        asciis = "".join(chr(b) if 32 <= b < 127 else "." for b in chunk)
        lines.append(f"{i:08x}  {hexs:<{width*3}}  |{asciis}|")
    return "\n".join(lines)


def try_preview_body(body: bytes, max_len: int = 200) -> str:
    if not body:
        return "(empty)"
    # Try JSON-looking
    stripped = body.lstrip()
    if stripped.startswith(b"{") or stripped.startswith(b"["):
        try:
            # Only try first 1k for JSON parse to avoid huge
            j = json.loads(body[:4096] if len(body) > 4096 else body)
            s = json.dumps(j, ensure_ascii=False)[:max_len]
            if len(body) > max_len:
                s += f" … (+{len(body)-max_len}B)"
            return s
        except Exception:
            pass
    # Try VarInt + string (common in status)
    try:
        txt = body.decode("utf-8")
        # Check if printable
        if all(32 <= ord(c) < 127 or c in "\n\r\t" for c in txt[:max_len]):
            s = txt[:max_len].replace("\n", "\\n")
            if len(txt) > max_len:
                s += f" … (+{len(txt)-max_len} chars)"
            return s
    except Exception:
        pass
    # Fallback hex preview
    preview = " ".join(f"{b:02x}" for b in body[:32])
    if len(body) > 32:
        preview += f" … ({len(body)}B total)"
    return preview


def read_varint(data: bytes, offset: int = 0) -> tuple[int, int]:
    """Read Minecraft VarInt from data[offset:], return (value, bytes_read)."""
    value = 0
    shift = 0
    pos = offset
    while pos < len(data):
        b = data[pos]
        value |= (b & 0x7F) << shift
        pos += 1
        if (b & 0x80) == 0:
            # sign extend?
            # Minecraft VarInt is signed 32; keep as unsigned for display Convert to signed if needed
            if value & (1 << 31):
                value -= 1 << 32
            return value, pos - offset
        shift += 7
        if shift >= 35:
            raise ValueError("VarInt too large")
    raise ValueError("truncated VarInt")


def print_human(cap: UmpkCapFile, args: argparse.Namespace) -> None:
    h = cap.header
    print(f"─ {h.path}")
    print(f"  magic: UMPKC1  version:{h.version}  protocol:{h.protocol}  frames:{h.frame_count} (parsed {len(cap.frames)})  size:{human_bytes(cap.raw_size)}")
    print(f"  recordedAt: {h.recorded_at_iso} ({h.recorded_at_ms} ms unix)")
    if cap.truncated or cap.error:
        print(f"  ⚠ {cap.error or 'truncated'}")
    if not cap.frames:
        print("  (no frames)")
        return

    # Determine filtering
    frames = cap.frames
    if args.filter_phase:
        want = PHASE_BY_NAME.get(args.filter_phase.lower())
        if want is not None:
            frames = [f for f in frames if f.phase == want]
    if args.filter_dir:
        wantd = DIR_BY_NAME.get(args.filter_dir.lower())
        if wantd is not None:
            frames = [f for f in frames if f.direction == wantd]

    if args.frame is not None:
        if 0 <= args.frame < len(cap.frames):
            f = cap.frames[args.frame]
            print(f"  ─ frame {args.frame}")
            print(f"    seq={f.sequence}  dir={f.direction_name}({f.direction})  phase={f.phase_name}({f.phase})  wireId={f.wire_id}  bodyLen={len(f.body)}  ticks={f.timestamp_ticks}")
            if args.hex:
                print(hexdump(f.body))
            else:
                print(f"    preview: {try_preview_body(f.body, 500)}")
                # Also show hex if body not previewable
                if len(f.body) <= 64:
                    print(f"    hex: {' '.join(f'{b:02x}' for b in f.body)}")
                # Try VarInt decode of first few bytes (wire framing often starts with length/string)
                try:
                    vi, n = read_varint(f.body, 0)
                    print(f"    varint[0]={vi} ({n} bytes)")
                    if len(f.body) > n + 2:
                        try:
                            vi2, n2 = read_varint(f.body, n)
                            print(f"    varint[{n}]={vi2} ({n2} bytes)")
                        except Exception:
                            pass
                except Exception:
                    pass
        else:
            print(f"  frame {args.frame} out of range (0..{len(cap.frames)-1})")
        return

    # Table listing
    limit = args.limit if args.limit else len(frames)
    shown = frames[:limit]
    print(f"  {'#':>5}  {'seq':>6}  {'dir':>11}  {'phase':>13}  {'wireId':>6}  {'len':>6}  {'ticks':>12}  preview")
    print(f"  {'─'*5}  {'─'*6}  {'─'*11}  {'─'*13}  {'─'*6}  {'─'*6}  {'─'*12}  {'─'*40}")
    for idx, f in enumerate(shown):
        # Map back to original index if filtered
        orig_idx = cap.frames.index(f) if len(frames) != len(cap.frames) else idx
        preview = try_preview_body(f.body, 80).replace("\n", "\\n")
        if len(preview) > 80:
            preview = preview[:77] + "..."
        print(f"  {orig_idx:5d}  {f.sequence:6d}  {f.direction_name:>11}  {f.phase_name:>13}  {f.wire_id:6d}  {len(f.body):6d}  {f.timestamp_ticks:12d}  {preview}")
    if len(frames) > limit:
        print(f"  … {len(frames)-limit} more frames (use --limit 0 for all)")
    # Summary by phase/dir
    from collections import Counter

    c_phase = Counter(f.phase_name for f in cap.frames)
    c_dir = Counter(f.direction_name for f in cap.frames)
    c_wire = Counter(f.wire_id for f in cap.frames if f.phase == 4)  # play
    print(f"  stats: by-phase {dict(c_phase)}  by-dir {dict(c_dir)}", end="")
    if c_wire:
        top = c_wire.most_common(5)
        print(f"  top-play-wireIds {top}", end="")
    print()


def emit_json(caps: List[UmpkCapFile], args: argparse.Namespace) -> None:
    out = []
    for cap in caps:
        h = cap.header
        frames = cap.frames
        if args.filter_phase:
            want = PHASE_BY_NAME.get(args.filter_phase.lower())
            if want is not None:
                frames = [f for f in frames if f.phase == want]
        if args.filter_dir:
            wantd = DIR_BY_NAME.get(args.filter_dir.lower())
            if wantd is not None:
                frames = [f for f in frames if f.direction == wantd]
        if args.frame is not None:
            if 0 <= args.frame < len(frames):
                frames = [frames[args.frame]]
            else:
                frames = []

        jframes = []
        for f in frames:
            jf = {
                "sequence": f.sequence,
                "direction": f.direction_name,
                "directionRaw": f.direction,
                "phase": f.phase_name,
                "phaseRaw": f.phase,
                "wireId": f.wire_id,
                "bodyLength": len(f.body),
                "timestampTicks": f.timestamp_ticks,
            }
            if args.hex:
                jf["bodyHex"] = f.body.hex()
            else:
                # include preview and optionally base64? keep hex for lossless
                jf["bodyPreview"] = try_preview_body(f.body, 300)
            jframes.append(jf)
            if args.limit and len(jframes) >= args.limit:
                break
        out.append(
            {
                "path": h.path,
                "header": {
                    "magic": "UMPKC1",
                    "version": h.version,
                    "protocol": h.protocol,
                    "frameCount": h.frame_count,
                    "recordedAtMs": h.recorded_at_ms,
                    "recordedAtIso": h.recorded_at_iso,
                    "rawSize": cap.raw_size,
                    "truncated": cap.truncated,
                    "error": cap.error,
                },
                "frames": jframes,
            }
        )
    json.dump(out, sys.stdout, indent=2, ensure_ascii=False)
    sys.stdout.write("\n")


def main() -> None:
    ap = argparse.ArgumentParser(
        description="Inspect UMPK .umpkcap capture files (MCC diagnostics & UMPK corpus).",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog=textwrap.dedent(
            """\
            examples:
              %(prog)s /tmp/mcc-diagnostics.zip
              %(prog)s fixtures/corpus/477/login-config.umpkcap --frame 0 --hex
              %(prog)s ./captures --filter-phase play --limit 20
              %(prog)s capture.umpkcap --json > out.json
            """
        ),
    )
    ap.add_argument("path", nargs="+", help=".umpkcap file, directory, or diagnostics .zip")
    ap.add_argument("--json", action="store_true", help="emit JSON instead of human table")
    ap.add_argument("--frame", type=int, default=None, help="dump single frame N in detail")
    ap.add_argument("--hex", action="store_true", help="with --frame, hex-dump body; with --json, include bodyHex")
    ap.add_argument("--limit", type=int, default=50, help="max frames to list (0=all, default 50)")
    ap.add_argument("--filter-phase", type=str, default=None, help="filter phase: handshake,status,login,configuration,play")
    ap.add_argument("--filter-dir", type=str, default=None, help="filter direction: clientbound,serverbound")
    args = ap.parse_args()

    caps: List[UmpkCapFile] = []
    zip_without_captures: List[Path] = []
    for raw in args.path:
        p = Path(raw)
        if not p.exists():
            print(f"not found: {raw}", file=sys.stderr)
            continue
        if p.is_file() and p.suffix == ".zip":
            try:
                found_in_zip = 0
                for entry_name, cap in iter_captures_in_zip(p):
                    caps.append(cap)
                    found_in_zip += 1
                if found_in_zip == 0:
                    zip_without_captures.append(p)
            except zipfile.BadZipFile as e:
                print(f"bad zip {p}: {e}", file=sys.stderr)
            except Exception as e:
                print(f"error reading zip {p}: {e}", file=sys.stderr)
        elif p.is_file() and p.suffix == ".umpkcap":
            cap = parse_file(p)
            caps.append(cap)
        elif p.is_dir():
            found = sorted(p.rglob("*.umpkcap"))
            if not found:
                # also check for zips inside dir (diagnostics bundles)
                zips = sorted(p.rglob("*.zip"))
                for zp in zips:
                    for entry_name, cap in iter_captures_in_zip(zp):
                        caps.append(cap)
                if not caps:
                    print(f"no .umpkcap found under {p}", file=sys.stderr)
                continue
            for fp in found:
                caps.append(parse_file(fp))
        else:
            # try glob expansion fallback
            import glob

            for g in glob.glob(raw, recursive=True):
                gp = Path(g)
                if gp.suffix == ".zip":
                    for entry_name, cap in iter_captures_in_zip(gp):
                        caps.append(cap)
                elif gp.suffix == ".umpkcap":
                    caps.append(parse_file(gp))

    if not caps:
        if zip_without_captures and not args.json:
            for zp in zip_without_captures:
                describe_zip_without_captures(zp)
            sys.exit(0)
        print("no captures found", file=sys.stderr)
        if zip_without_captures:
            print(f"hint: {len(zip_without_captures)} zip(s) contained no .umpkcap (early failure → no capturable frames).", file=sys.stderr)
        sys.exit(2)

    if args.json:
        emit_json(caps, args)
    else:
        for cap in caps:
            print_human(cap, args)


if __name__ == "__main__":
    main()
