"""Read-only local tool discovery. Never launches an arbitrary Unity editor."""
from dataclasses import dataclass
import os
from pathlib import Path
import plistlib
import re
import shutil

HUB_ROOTS = (Path('/Applications/Unity/Hub/Editor'), Path.home() / 'Applications/Unity/Hub/Editor')
BLENDER_APPS = (Path('/Applications/Blender.app/Contents/MacOS/Blender'),
                Path.home() / 'Applications/Blender.app/Contents/MacOS/Blender')


@dataclass(frozen=True)
class Tool:
    status: str
    path: Path | None = None
    version: str | None = None
    detail: str = ''


def executable(path):
    return path.is_file() and os.access(path, os.X_OK)


def project_version(root):
    text = (Path(root) / 'unity/ProjectSettings/ProjectVersion.txt').read_text()
    match = re.search(r'^m_EditorVersion:\s*(\S+)', text, re.M)
    if not match:
        raise ValueError('ProjectVersion.txt does not contain m_EditorVersion')
    return match.group(1)


def app_version(binary):
    # Resolve symlinks from PATH, then use bundle metadata without starting an editor.
    binary = Path(binary).resolve()
    for parent in binary.parents:
        if parent.name == 'Contents':
            try:
                data = plistlib.loads((parent / 'Info.plist').read_bytes())
                return data.get('CFBundleVersion') or data.get('CFBundleShortVersionString')
            except (OSError, plistlib.InvalidFileException):
                return None
    return None


def inspect_unity(path, expected, source):
    path = Path(path).expanduser().resolve()
    if not executable(path):
        return Tool('MISSING', path, detail=f'{source} is not executable; no fallback was selected')
    version = app_version(path)
    if version != expected:
        return Tool('VERSION MISMATCH', path, version,
                    f'{source}: project requires {expected}; ' + ('version could not be verified' if version is None else f'found {version}'))
    return Tool('FOUND', path, version, source)


def discover_unity(root, env=None, hub_roots=None, which=shutil.which):
    env = os.environ if env is None else env
    roots = HUB_ROOTS if hub_roots is None else hub_roots
    expected = project_version(root)
    if env.get('UNITY_EDITOR'):
        return inspect_unity(env['UNITY_EDITOR'], expected, 'UNITY_EDITOR override')
    candidates = [Path(base) / expected / 'Unity.app/Contents/MacOS/Unity' for base in roots]
    for candidate in candidates:
        if executable(candidate):
            return inspect_unity(candidate, expected, 'matching Unity Hub installation')
    on_path = which('Unity')
    if on_path:
        return inspect_unity(on_path, expected, 'PATH')
    other_versions = []
    for base in roots:
        for candidate in sorted(Path(base).glob('*/Unity.app/Contents/MacOS/Unity')):
            if executable(candidate):
                other_versions.append(app_version(candidate) or candidate.parents[3].name)
    if other_versions:
        return Tool('VERSION MISMATCH', detail=f'Project requires {expected}; installed: {", ".join(other_versions)}. No editor selected.')
    return Tool('MISSING', detail=f'Install Unity {expected} through Unity Hub or set UNITY_EDITOR')


def discover_blender(env=None, app_paths=None, which=shutil.which, expected=None):
    env = os.environ if env is None else env
    paths = BLENDER_APPS if app_paths is None else app_paths
    override = env.get('BLENDER')
    candidates = [override] if override else list(paths) + [which('blender')]
    for value in candidates:
        if not value:
            continue
        path = Path(value).expanduser().resolve()
        if executable(path):
            version = app_version(path)
            if expected and version != expected:
                return Tool('VERSION MISMATCH', path, version, f'Tested Blender baseline is {expected}; version is {version or "unverified"}')
            return Tool('FOUND', path, version, 'BLENDER override' if override else 'local installation')
    return Tool('MISSING', detail='BLENDER override is not executable; no fallback selected' if override else 'Install Blender or set BLENDER')


def discover_adb(unity, env=None, which=shutil.which):
    env = os.environ if env is None else env
    override = env.get('ADB')
    if override:
        path = Path(override).expanduser().resolve()
        return Tool('FOUND' if executable(path) else 'MISSING', path, detail='ADB override; no fallback selected')
    if unity.status == 'FOUND' and unity.path:
        # Hub modules are siblings of Unity.app on current macOS installs. Also
        # recognize older embedded layouts; never look under an unmatched editor.
        app = next((p for p in unity.path.parents if p.name == 'Unity.app'), None)
        if app:
            for base in [app.parent / 'PlaybackEngines', app / 'Contents/PlaybackEngines']:
                path = base / 'AndroidPlayer/SDK/platform-tools/adb'
                if executable(path):
                    return Tool('FOUND', path, detail='selected Unity Android SDK')
    path = which('adb')
    if path and executable(Path(path)):
        return Tool('FOUND', Path(path).resolve(), detail='PATH (does not prove Unity Android module is installed)')
    return Tool('MISSING', detail='Install Android Build Support, Android SDK & NDK Tools and OpenJDK for the selected editor in Unity Hub')
