#!/usr/bin/env python3
"""Read-only discovery: explicit overrides, matching macOS app installs, then PATH."""
from pathlib import Path
import shutil
import subprocess
from tool_discovery import discover_unity, discover_blender, discover_adb, project_version

ROOT = Path(__file__).resolve().parents[2]


def main():
    print('Project Unity:', project_version(ROOT), flush=True)
    for name in ['git', 'git-lfs']:
        path = shutil.which(name)
        print(f'{name}: {"FOUND" if path else "MISSING"} {path or ""}', flush=True)
    unity = discover_unity(ROOT)
    for name, tool in [('Unity', unity), ('Blender', discover_blender(expected='5.2.1')), ('adb', discover_adb(unity))]:
        print(f'{name}: {tool.status} {tool.path or ""} {tool.version or ""}\n  {tool.detail}', flush=True)
    subprocess.run(['git', 'status', '--short'], cwd=ROOT, check=True)


if __name__ == '__main__':
    main()
