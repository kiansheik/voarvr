#!/usr/bin/env python3
"""Read-only tool discovery; missing tools are reported without installing anything."""
import os
from pathlib import Path
import shutil
import subprocess

ROOT = Path(__file__).resolve().parents[2]
print((ROOT / "unity/ProjectSettings/ProjectVersion.txt").read_text().strip())
for name, override in [("git", None), ("git-lfs", None), ("Unity", "UNITY_EDITOR"),
                       ("blender", "BLENDER"), ("adb", "ADB")]:
    path = os.environ.get(override, "") if override else ""
    path = path or shutil.which(name)
    valid = path and Path(path).is_file() and os.access(path, os.X_OK)
    print(f"{name}: {path if valid else 'MISSING'}" + (f" (override: {override})" if override else ""))
subprocess.run(["git", "status", "--short"], cwd=ROOT, check=True)
