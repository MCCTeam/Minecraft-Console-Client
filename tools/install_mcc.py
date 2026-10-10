#!/usr/bin/env python3
"""Install one MCC 2.0 release archive and verify its checksum."""
import argparse
import hashlib
import json
import os
import platform
import re
import shlex
import shutil
import struct
import tarfile
import tempfile
import urllib.request
import urllib.parse
import zipfile
from pathlib import Path, PurePosixPath

REPOSITORY = "MCCTeam/Minecraft-Console-Client"

def detect_target():
    system = platform.system()
    machine = platform.machine().lower()
    bits = struct.calcsize("P") * 8
    if machine in ("aarch64", "arm64"):
        arch = "arm64" if bits == 64 else "arm"
    elif machine.startswith("arm"):
        arch = "arm"
    elif machine in ("x86_64", "amd64"):
        arch = "x64" if bits == 64 else "x86"
    elif machine in ("x86", "i386", "i486", "i586", "i686"):
        arch = "x86"
    else:
        raise ValueError(f"Unsupported process architecture: {machine} ({bits} bit)")
    if system == "Darwin" and arch in ("x64", "arm64"):
        return "osx-" + arch
    if system == "Windows" and arch in ("x86", "x64", "arm64"):
        return "win-" + arch
    if system == "Linux" and arch in ("x64", "arm64", "arm"):
        musl = platform.libc_ver()[0] == "musl" or any(Path("/lib").glob("ld-musl-*.so.1"))
        return ("linux-musl-" if musl else "linux-") + arch
    raise ValueError(f"Unsupported operating system/process combination: {system}/{arch}")

def fetch(url):
    request = urllib.request.Request(url, headers={"User-Agent": "MCC-2-installer", "Accept": "application/vnd.github+json"})
    with urllib.request.urlopen(request, timeout=60) as response:
        return response.read()

def unpack(archive, destination):
    destination = Path(destination)
    def safe(name):
        p = PurePosixPath(name.replace("\\", "/"))
        if p.is_absolute() or ".." in p.parts or ":" in name:
            raise ValueError(f"Unsafe archive path: {name}")
        target = destination.joinpath(*p.parts)
        if not target.resolve().is_relative_to(destination.resolve()):
            raise ValueError(f"Archive path escapes installation: {name}")
        return target
    if str(archive).endswith(".zip"):
        with zipfile.ZipFile(archive) as package:
            for entry in package.infolist():
                target = safe(entry.filename)
                if (entry.external_attr >> 16) & 0o170000 == 0o120000:
                    raise ValueError("Archive links are not allowed")
                if entry.is_dir():
                    target.mkdir(parents=True, exist_ok=True)
                else:
                    target.parent.mkdir(parents=True, exist_ok=True)
                    with package.open(entry) as source, target.open("xb") as output:
                        shutil.copyfileobj(source, output)
    else:
        with tarfile.open(archive, "r:gz") as package:
            for entry in package:
                target = safe(entry.name)
                if entry.isdir():
                    target.mkdir(parents=True, exist_ok=True)
                elif entry.isfile():
                    target.parent.mkdir(parents=True, exist_ok=True)
                    with package.extractfile(entry) as source, target.open("xb") as output:
                        shutil.copyfileobj(source, output)
                    target.chmod(entry.mode & 0o777)
                else:
                    raise ValueError("Archive links and device files are not allowed")

def install(release, target, destination, download=fetch):
    tag = release["tag_name"]
    if not re.fullmatch(r"v?2\.\d+\.\d+(?:-[0-9A-Za-z.-]+)?", tag):
        raise ValueError("The selected release is not MCC 2.0. Build from source until an MCC 2.0 release is published.")
    extension = ".zip" if target.startswith("win-") else ".tar.gz"
    name = f"Mcc-{tag}-{target}{extension}"
    assets = {asset["name"]: asset["browser_download_url"] for asset in release["assets"]}
    if name not in assets or "SHA256SUMS" not in assets:
        raise ValueError(f"Release {tag} must contain {name} and SHA256SUMS")
    checksums = download(assets["SHA256SUMS"]).decode("utf-8")
    hashes = {}
    for line in checksums.splitlines():
        match = re.fullmatch(r"([0-9a-fA-F]{64})\s+\*?(.+)", line)
        if match:
            hashes[match[2]] = match[1].lower()
    if name not in hashes:
        raise ValueError(f"No checksum for {name}")
    destination = Path(destination).expanduser().resolve()
    version = destination/"releases"/tag/target
    if version.exists():
        raise ValueError(f"Already installed: {version}. Select another version or installation directory.")
    payload = download(assets[name])
    if hashlib.sha256(payload).hexdigest() != hashes[name]:
        raise ValueError("Archive checksum does not match. No installation files were changed.")
    destination.mkdir(parents=True, exist_ok=True)
    with tempfile.TemporaryDirectory(prefix=".mcc-install-", dir=destination) as scratch:
        archive = Path(scratch)/name
        archive.write_bytes(payload)
        stage = Path(scratch)/"payload"
        stage.mkdir()
        unpack(archive, stage)
        executable = "Mcc.Cli.exe" if target.startswith("win-") else "Mcc.Cli"
        if not (stage/executable).is_file():
            raise ValueError("Archive does not contain the MCC executable")
        version.parent.mkdir(parents=True, exist_ok=True)
        stage.rename(version)
    if target.startswith("win-"):
        launcher = destination/"mcc.cmd"
        binary = '"%~dp0releases\\' + tag + '\\' + target + '\\Mcc.Cli.exe"'
        modes = ['run', 'lint', 'format', '--validate-plugin', '--validate-marketplace', '--help', '--help-short']
        launcher.write_text('@echo off\r\ncd /d "%~dp0"\r\n' + ''.join('if "%~1"=="' + mode + '" goto tool\r\n' for mode in modes) + binary + ' --configurations "%~dp0configurations" %*\r\nexit /b %errorlevel%\r\n:tool\r\n' + binary + ' %*\r\n')
    else:
        (version/"Mcc.Cli").chmod(0o755)
        launcher = destination/"mcc"
        binary = shlex.quote(str(version/"Mcc.Cli"))
        launcher.write_text('#!/bin/sh\ncd ' + shlex.quote(str(destination)) + ' || exit 1\ncase "${1:-}" in\n  run|lint|format|--validate-plugin|--validate-marketplace|--help|--help-short) exec ' + binary + ' "$@" ;;\n  *) exec ' + binary + ' --configurations ' + shlex.quote(str(destination/"configurations")) + ' "$@" ;;\nesac\n')
        launcher.chmod(0o755)
    return launcher

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--version", default="latest", help="Release tag, including prereleases, or latest stable")
    parser.add_argument("--dest", default=str(Path.home()/".local/share/mcc"))
    parser.add_argument("--rid", help="Override process platform detection")
    parser.add_argument("--print-rid", action="store_true")
    args = parser.parse_args()
    target = args.rid or detect_target()
    if not re.fullmatch(r"(?:win-(?:x86|x64|arm64)|osx-(?:x64|arm64)|linux-(?:musl-)?(?:x64|arm64|arm))", target):
        raise ValueError("Unsupported RID: " + target)
    if args.print_rid:
        print(target)
        return
    endpoint = "latest" if args.version == "latest" else "tags/" + urllib.parse.quote(args.version, safe="")
    release = json.loads(fetch(f"https://api.github.com/repos/{REPOSITORY}/releases/{endpoint}"))
    launcher = install(release, target, args.dest)
    print(f"Installed {release['tag_name']} for {target}.")
    print(f"Run: {launcher} --help")
    print("Existing configuration, plugins and scripts were preserved.")

if __name__ == "__main__":
    try:
        main()
    except (ValueError, OSError, KeyError) as error:
        raise SystemExit("MCC install failed: " + str(error))
