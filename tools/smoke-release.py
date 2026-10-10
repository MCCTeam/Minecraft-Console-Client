#!/usr/bin/env python3
"""Execute a release archive and compile a source plugin without an installed SDK."""
import argparse
import os
import shutil
import subprocess
import tempfile
import tomllib
from pathlib import Path

from install_mcc import unpack


def check_release(archive: Path, stamp: str) -> None:
    with tempfile.TemporaryDirectory(prefix='mcc-release-smoke-') as temporary:
        root = Path(temporary)
        payload = root/'payload'
        payload.mkdir()
        unpack(archive, payload)
        binary = 'Mcc.Cli.exe' if os.name == 'nt' else 'Mcc.Cli'
        executable = payload/binary
        assert {path.relative_to(payload).as_posix() for path in payload.rglob('*') if path.is_file()} == {binary, 'LICENSE.md'}
        executable.chmod(0o755)
        plugin = root/'source-plugin'
        shutil.copytree(Path(__file__).with_name('release-smoke-plugin'), plugin)
        inputs = root/'input.txt'
        inputs.write_text('\n'.join([
            '/plugins marketplace list',
            '/plugins load source-plugin',
            '/plugins doctor',
            '/release-smoke',
            '/plugins reload release-smoke',
            '/release-smoke',
            '/plugins unload release-smoke',
            '/plugins doctor',
            'exit',
        ])+'\n')
        environment = os.environ.copy()
        environment.pop('MCC_PLUGINS', None)
        environment.update(
            MCC_FILE_INPUT='1', MCC_INPUT_FILE=str(inputs),
            DOTNET_BUNDLE_EXTRACT_BASE_DIR=str(root/'bundle-cache'),
            DOTNET_ROOT=str(root/'no-dotnet'),
            DOTNET_MULTILEVEL_LOOKUP='0',
        )
        environment['PATH'] = str(root/'empty-path')
        subprocess.run([str(executable), '--help-short'], cwd=root, env=environment, check=True, timeout=60)
        result = subprocess.run([
            str(executable), 'ReleaseSmoke', '-', '--configurations', str(root/'configurations'),
            '--connection.auto-connect=false',
        ], cwd=root, env=environment, check=True, capture_output=True, text=True, timeout=120)
        transcript = result.stdout + result.stderr
        print(transcript)
        assert f'Minecraft Console Client v{stamp} -' in transcript
        assert 'Plugins: 1 discovered, 1 loaded.' in transcript, 'Source plugin did not load.'
        assert transcript.count('RELEASE_SOURCE_PLUGIN_OK') == 2, 'Source plugin command failed before or after reload.'
        assert 'Plugins: 1 discovered, 0 loaded.' in transcript, 'Source plugin did not unload.'
        registry = tomllib.loads((root/'configurations/marketplaces.toml').read_text())
        assert registry['schema-version'] == 2
        assert len(registry['marketplaces']) == 1
        assert registry['marketplaces'][0]['id'] == 'official'
        assert registry['marketplaces'][0]['auto-update'] == 'off'
        assert list((root/'bundle-cache').rglob('DMCBK.PluginSdk.dll')), 'Bundled plugin references were not extracted.'
        print('Single-file startup, source-plugin compilation and reload passed without an installed .NET runtime or SDK.')


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('archive', type=Path)
    parser.add_argument('--stamp', required=True)
    arguments = parser.parse_args()
    check_release(arguments.archive.resolve(), arguments.stamp)
