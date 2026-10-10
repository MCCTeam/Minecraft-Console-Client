#!/usr/bin/env python3
"""Offline release-installer checks. No GitHub requests or real installs."""
import hashlib
import importlib.util
import io
import subprocess
import sys
import tarfile
import tempfile
import unittest
import zipfile
from pathlib import Path
from unittest.mock import patch

spec = importlib.util.spec_from_file_location('install_mcc', Path(__file__).with_name('install_mcc.py'))
installer = importlib.util.module_from_spec(spec)
spec.loader.exec_module(installer)

def release(payload, rid='linux-x64', expected=None):
    name = f'Mcc-v2.0.0-preview.1-{rid}' + ('.zip' if rid.startswith('win-') else '.tar.gz')
    checksums = (expected or hashlib.sha256(payload).hexdigest()) + '  ' + name + '\n'
    metadata = {'tag_name': 'v2.0.0-preview.1', 'assets': [
        {'name': name, 'browser_download_url': 'fixture://selected'},
        {'name': 'Mcc-v2.0.0-preview.1-osx-arm64.tar.gz', 'browser_download_url': 'fixture://other'},
        {'name': 'SHA256SUMS', 'browser_download_url': 'fixture://hashes'},
    ]}
    requests=[]
    def download(url):
        requests.append(url)
        return checksums.encode() if url == 'fixture://hashes' else payload
    return metadata, download, requests

def archive(extra=None, windows=False, single_file=False):
    stream=io.BytesIO()
    files={'Mcc.Cli.exe' if windows else 'Mcc.Cli':b'executable'}
    if not single_file:
        files['Mcc.Cli.dll']=b'assembly'
    if extra:
        files.update(extra)
    if windows:
        with zipfile.ZipFile(stream, 'w') as package:
            for name,data in files.items():
                package.writestr(name,data)
    else:
        with tarfile.open(fileobj=stream, mode='w:gz') as package:
            for name,data in files.items():
                entry=tarfile.TarInfo(name);entry.size=len(data);entry.mode=0o755
                package.addfile(entry,io.BytesIO(data))
    return stream.getvalue()

class InstallTests(unittest.TestCase):
    def test_single_file_release_installs_without_loose_assemblies(self):
        for rid in ('linux-x64', 'win-x64'):
            with self.subTest(rid=rid), tempfile.TemporaryDirectory() as scratch:
                metadata, download, _ = release(archive(windows=rid.startswith('win-'), single_file=True), rid)
                launcher = installer.install(metadata, rid, scratch, download)
                self.assertTrue(launcher.is_file())
                payload = Path(scratch)/'releases'/'v2.0.0-preview.1'/rid
                self.assertEqual(1, len(list(payload.iterdir())))

    def test_selected_asset_only_and_user_data_preserved(self):
        metadata,download,requests=release(archive())
        with tempfile.TemporaryDirectory() as scratch:
            destination=Path(scratch)/'MCC with spaces'
            (destination/'configurations').mkdir(parents=True)
            existing=destination/'configurations/client.toml';existing.write_text('do not replace')
            launcher=installer.install(metadata,'linux-x64',destination,download)
            self.assertTrue(launcher.exists())
            self.assertEqual('do not replace',existing.read_text())
            self.assertEqual(['fixture://hashes','fixture://selected'],requests)
            self.assertIn('MCC with spaces',launcher.read_text())
            with self.assertRaisesRegex(ValueError,'Already installed'):
                installer.install(metadata,'linux-x64',destination,download)

    def test_hash_failure_changes_nothing(self):
        metadata,download,_=release(archive(),expected='0'*64)
        with tempfile.TemporaryDirectory() as scratch:
            destination=Path(scratch)/'missing'
            with self.assertRaisesRegex(ValueError,'checksum'):
                installer.install(metadata,'linux-x64',destination,download)
            self.assertFalse(destination.exists())

    def test_tar_traversal_is_rejected(self):
        metadata,download,_=release(archive({'../outside':b'bad'}))
        with tempfile.TemporaryDirectory() as scratch:
            with self.assertRaisesRegex(ValueError,'Unsafe archive'):
                installer.install(metadata,'linux-x64',Path(scratch)/'mcc',download)
            self.assertFalse((Path(scratch)/'outside').exists())

    def test_zip_traversal_is_rejected(self):
        metadata,download,_=release(archive({'../outside':b'bad'},True),'win-x64')
        with tempfile.TemporaryDirectory() as scratch:
            with self.assertRaisesRegex(ValueError,'Unsafe archive'):
                installer.install(metadata,'win-x64',Path(scratch)/'mcc',download)

    def test_old_release_is_rejected(self):
        metadata,download,requests=release(archive());metadata['tag_name']='v1.0.0'
        with self.assertRaisesRegex(ValueError,'not MCC 2.0'):
            installer.install(metadata,'linux-x64','unused',download)
        self.assertEqual([],requests)

    def test_windows_archive_has_launcher(self):
        metadata,download,_=release(archive(windows=True),'win-x64')
        with tempfile.TemporaryDirectory() as scratch:
            launcher=installer.install(metadata,'win-x64',scratch,download)
            self.assertEqual('mcc.cmd',launcher.name)
            self.assertIn('Mcc.Cli.exe',launcher.read_text())

    def test_tar_links_are_rejected(self):
        stream=io.BytesIO()
        with tarfile.open(fileobj=stream,mode='w:gz') as package:
            entry=tarfile.TarInfo('link');entry.type=tarfile.SYMTYPE;entry.linkname='/tmp/outside';package.addfile(entry)
        metadata,download,_=release(stream.getvalue())
        with tempfile.TemporaryDirectory() as scratch:
            with self.assertRaisesRegex(ValueError,'links'):
                installer.install(metadata,'linux-x64',scratch,download)

    def test_x86_process_on_x64_windows_uses_x86(self):
        with patch.object(installer.platform,'system',return_value='Windows'),patch.object(installer.platform,'machine',return_value='AMD64'),patch.object(installer.struct,'calcsize',return_value=4):
            self.assertEqual('win-x86',installer.detect_target())

    def test_launcher_preserves_first_argument_for_beacon_tools(self):
        executable = b'#!/bin/sh\nprintf "%s\\n" "$@"\n'
        metadata, download, _ = release(archive({'Mcc.Cli': executable}))
        with tempfile.TemporaryDirectory() as scratch:
            launcher = installer.install(metadata, 'linux-x64', scratch, download)
            result = subprocess.run([str(launcher), 'lint', '/tmp/hello.bcn'], capture_output=True, text=True, check=True)
            self.assertEqual(['lint', '/tmp/hello.bcn'], result.stdout.splitlines())
            result = subprocess.run([str(launcher), 'Steve', '-'], capture_output=True, text=True, check=True)
            self.assertEqual(['--configurations', str(Path(scratch)/'configurations'), 'Steve', '-'], result.stdout.splitlines())

    def test_public_shell_contains_the_canonical_installer(self):
        source=Path(__file__).with_name('install_mcc.py').read_text()
        shell=Path(__file__).resolve().parents[1]/'docs/.vuepress/public/install.sh'
        self.assertIn(source,shell.read_text())

class ReleasePackageTests(unittest.TestCase):
    def test_single_file_package_keeps_only_binary_and_license(self):
        with tempfile.TemporaryDirectory() as scratch:
            root = Path(scratch)
            payload = root/'publish'
            payload.mkdir()
            (payload/'Mcc.Cli').write_bytes(b'executable')
            (payload/'LICENSE.md').write_text('MIT license')
            result = self.package(payload, root/'releases')
            self.assertEqual(0, result.returncode, result.stderr)
            archive = root/'releases'/'Mcc-v2.0.0-preview.1-linux-x64.tar.gz'
            with tarfile.open(archive) as package:
                self.assertEqual({'Mcc.Cli', 'LICENSE.md'}, set(package.getnames()))
            expected = hashlib.sha256(archive.read_bytes()).hexdigest()
            self.assertEqual(expected+'  '+archive.name+'\n', (root/'releases'/'SHA256SUMS').read_text())

    def test_single_file_package_rejects_loose_dependencies(self):
        with tempfile.TemporaryDirectory() as scratch:
            root = Path(scratch)
            payload = root/'publish'
            payload.mkdir()
            (payload/'Mcc.Cli').write_bytes(b'executable')
            (payload/'dependency.dll').write_bytes(b'loose assembly')
            result = self.package(payload, root/'releases')
            self.assertNotEqual(0, result.returncode)
            self.assertIn('loose dependencies', result.stderr)
            self.assertFalse((root/'releases').exists())

    @staticmethod
    def package(payload, output):
        return subprocess.run([sys.executable, str(Path(__file__).with_name('package-release.py')), str(payload),
            '--tag', 'v2.0.0-preview.1', '--rid', 'linux-x64', '--single-file', '--output', str(output)],
            capture_output=True, text=True)

if __name__=='__main__':
    unittest.main()
