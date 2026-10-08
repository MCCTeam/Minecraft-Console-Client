#!/usr/bin/env python3
"""Archive a complete MCC publish directory and write release checksums."""
import argparse
import hashlib
import re
import tarfile
import zipfile
from pathlib import Path

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('directory', type=Path)
parser.add_argument('--tag', required=True)
parser.add_argument('--rid', required=True)
parser.add_argument('--output', type=Path, default=Path('artifacts/releases'))
args = parser.parse_args()
if not re.fullmatch(r'v?2\.\d+\.\d+(?:-[0-9A-Za-z.-]+)?', args.tag):
    parser.error('Use an MCC 2.0 release tag, such as v2.0.0-preview.1')
if not re.fullmatch(r'(?:win-(?:x86|x64|arm64)|osx-(?:x64|arm64)|linux-(?:musl-)?(?:x64|arm64|arm))', args.rid):
    parser.error('Unsupported RID')
binary = 'Mcc.Cli.exe' if args.rid.startswith('win-') else 'Mcc.Cli'
for name in [binary, 'Mcc.Cli.dll', 'Mcc.Cli.deps.json', 'Mcc.Cli.runtimeconfig.json']:
    if not (args.directory/name).is_file():
        parser.error('Publish directory is missing ' + name)
args.output.mkdir(parents=True, exist_ok=True)
extension = '.zip' if args.rid.startswith('win-') else '.tar.gz'
archive = args.output/f'Mcc-{args.tag}-{args.rid}{extension}'
if archive.exists():
    parser.error('Archive already exists. Published releases are immutable.')
files = sorted(p for p in args.directory.rglob('*') if p.is_file())
if any(p.is_symlink() for p in args.directory.rglob('*')):
    parser.error('Publish directory must not contain symlinks')
if extension == '.zip':
    with zipfile.ZipFile(archive, 'x', compression=zipfile.ZIP_DEFLATED) as package:
        for p in files:
            package.write(p, p.relative_to(args.directory).as_posix())
else:
    with tarfile.open(archive, 'x:gz') as package:
        for p in files:
            package.add(p, arcname=p.relative_to(args.directory).as_posix(), recursive=False)
checksums = []
for p in sorted(args.output.glob('Mcc-*')):
    if p.suffix not in ('.zip','.gz'):
        continue
    checksums.append(hashlib.sha256(p.read_bytes()).hexdigest() + '  ' + p.name)
(args.output/'SHA256SUMS').write_text('\n'.join(checksums) + '\n')
print(archive)
