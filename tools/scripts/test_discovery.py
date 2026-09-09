"""Discovery tests use temporary fake app bundles, never the developer's paths."""
from pathlib import Path
import plistlib
import tempfile
import unittest
from tool_discovery import Tool, discover_unity, discover_blender, discover_adb


class DiscoveryTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name).resolve()
        self.hub = self.root / 'Hub'
        project = self.root / 'unity/ProjectSettings'
        project.mkdir(parents=True)
        (project / 'ProjectVersion.txt').write_text('m_EditorVersion: 6000.6.0f1\n')

    def binary(self, path):
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text('#!/bin/sh\nexit 0\n')
        path.chmod(0o755)
        return path

    def app(self, base, name, version):
        contents = base / f'{name}.app/Contents'
        binary = self.binary(contents / f'MacOS/{name}')
        (contents / 'Info.plist').write_bytes(plistlib.dumps({'CFBundleVersion': version}))
        return binary

    def unity(self, env=None, path=None):
        return discover_unity(self.root, env=env or {}, hub_roots=[self.hub], which=lambda _: path)

    def test_exact_hub_match_wins_over_wrong_path(self):
        expected = self.app(self.hub / '6000.6.0f1', 'Unity', '6000.6.0f1')
        wrong = self.app(self.root / 'other', 'Unity', '6000.3.21f1')
        result = self.unity(path=str(wrong))
        self.assertEqual((result.status, result.path), ('FOUND', expected))

    def test_override_is_authoritative_even_if_wrong_or_missing(self):
        self.app(self.hub / '6000.6.0f1', 'Unity', '6000.6.0f1')
        wrong = self.app(self.root / 'override', 'Unity', '6000.3.21f1')
        self.assertEqual(self.unity({'UNITY_EDITOR': str(wrong)}).status, 'VERSION MISMATCH')
        self.assertEqual(self.unity({'UNITY_EDITOR': str(self.root / 'missing')}).status, 'MISSING')

    def test_other_installed_version_is_not_selected(self):
        self.app(self.hub / '6000.3.21f1', 'Unity', '6000.3.21f1')
        result = self.unity()
        self.assertEqual(result.status, 'VERSION MISMATCH')
        self.assertIsNone(result.path)

    def test_missing_editor(self):
        self.assertEqual(self.unity().status, 'MISSING')

    def test_unverified_path_editor_is_not_accepted(self):
        unknown = self.binary(self.root / 'bin/Unity')
        self.assertEqual(self.unity(path=str(unknown)).status, 'VERSION MISMATCH')

    def test_symlink_to_matching_bundle(self):
        binary = self.app(self.root / 'custom', 'Unity', '6000.6.0f1')
        link = self.root / 'unity-cli-link'
        link.symlink_to(binary)
        self.assertEqual(self.unity(path=str(link)).status, 'FOUND')

    def test_bundle_version_is_checked_not_directory_name(self):
        self.app(self.hub / '6000.6.0f1', 'Unity', '6000.3.21f1')
        self.assertEqual(self.unity().status, 'VERSION MISMATCH')

    def test_blender_app_and_override(self):
        binary = self.app(self.root, 'Blender', '5.2.1')
        discover = lambda env: discover_blender(env=env, app_paths=[binary], which=lambda _: None, expected='5.2.1')
        self.assertEqual(discover({}).status, 'FOUND')
        self.assertEqual(discover({'BLENDER': str(self.root / 'missing')}).status, 'MISSING')
        wrong = self.app(self.root / 'old', 'Blender', '4.5.0')
        self.assertEqual(discover({'BLENDER': str(wrong)}).status, 'VERSION MISMATCH')

    def test_adb_from_selected_hub_module(self):
        editor = self.app(self.hub / '6000.6.0f1', 'Unity', '6000.6.0f1')
        adb = self.binary(self.hub / '6000.6.0f1/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb')
        found = discover_adb(Tool('FOUND', editor), env={}, which=lambda _: None)
        self.assertEqual(found.path, adb)
        self.assertEqual(found.status, 'FOUND')

    def test_missing_module_and_adb_override(self):
        editor = self.app(self.hub / '6000.6.0f1', 'Unity', '6000.6.0f1')
        result = discover_adb(Tool('FOUND', editor), env={}, which=lambda _: None)
        self.assertEqual(result.status, 'MISSING')
        self.assertIn('Android Build Support', result.detail)
        custom = self.binary(self.root / 'custom-adb')
        self.assertEqual(discover_adb(Tool('FOUND', editor), env={'ADB': str(custom)}, which=lambda _: None).path, custom)
        self.assertEqual(discover_adb(Tool('FOUND', editor), env={'ADB': str(self.root / 'missing')}, which=lambda _: str(custom)).status, 'MISSING')


if __name__ == '__main__':
    unittest.main()
