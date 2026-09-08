#!/usr/bin/env python3
"""Offline structural checks; this does not compile C# or validate Unity imports."""
import ast
import json
import os
from pathlib import Path
import re
import subprocess

ROOT = Path(__file__).resolve().parents[2]
ASSETS = ROOT / "unity/Assets"
errors = []
files = []
for directory, subdirs, names in os.walk(ROOT):
    subdirs[:] = [name for name in subdirs if name not in {
        ".git", "Library", "Temp", "Logs", "obj", "UserSettings", "artifacts",
        "builds", "Builds", "generated", "exports", "__pycache__", ".venv"
    }]
    files.extend(Path(directory) / name for name in names)
unresolved = {}
for path in files:
    try:
        if path.suffix in {".json", ".asmdef"}:
            json.loads(path.read_text())
        if path.suffix == ".py":
            ast.parse(path.read_text(), filename=str(path))
    except (ValueError, SyntaxError) as error:
        errors.append(str(error))
metadata = {}
for path in ASSETS.rglob("*.meta"):
    match = re.search(r"^guid: ([a-f0-9]{32})$", path.read_text(), re.M)
    if not match:
        errors.append(f"Invalid GUID: {path}")
        continue
    guid = match.group(1)
    if guid in metadata:
        errors.append(f"Duplicate GUID: {path} / {metadata[guid]}")
    metadata[guid] = path
    target = Path(str(path)[:-5])
    # Folder-only .meta files preserve deliberately empty Unity directories in Git.
    if not target.exists() and "folderAsset: yes" not in path.read_text():
        errors.append(f"Orphan metadata: {path}")
for path in ASSETS.rglob("*"):
    if path.suffix != ".meta" and not path.name.startswith(".") and not Path(str(path) + ".meta").is_file():
        errors.append(f"Missing metadata: {path}")
for path in files:
    if path.suffix in {".unity", ".mat", ".asset"}:
        for guid in re.findall(r"guid: ([a-f0-9]{32})", path.read_text()):
            if guid not in metadata and not guid.startswith("0000000000000000"):
                unresolved.setdefault(guid, []).append(path)
    if path.suffix == ".md":
        for link in re.findall(r"\[[^\]]*\]\(([^)]+)\)", path.read_text()):
            if "://" in link or link.startswith("#") or link.startswith("mailto:"):
                continue
            target = link.split("#")[0]
            if target and not (path.parent / target).exists():
                errors.append(f"Broken documentation link: {path.relative_to(ROOT)} -> {link}")
if unresolved:
    # Imported project settings may refer to installed package assets. Resolve those
    # only when needed; never treat the mere existence of Library as validation.
    for path in (ROOT / "unity/Library/PackageCache").rglob("*.meta"):
        match = re.search(r"^guid: ([a-f0-9]{32})$", path.read_text(), re.M)
        if match:
            unresolved.pop(match.group(1), None)
        if not unresolved:
            break
    for guid, paths in unresolved.items():
        errors.append(f"Unresolved GUID: {guid} in {paths}")
for ignored in ["unity/Library/probe", "unity/Temp/probe", "unity/Logs/probe", "unity/UserSettings/probe", "builds/probe.apk", "artifacts/probe.xml", "blender/source/probe.blend1"]:
    if subprocess.run(["git", "check-ignore", "-q", "--no-index", ignored], cwd=ROOT).returncode != 0:
        errors.append(f"Not ignored: {ignored}")
for tracked in ["unity/Assets/Scenes/Bootstrap/Bootstrap.unity", "unity/Packages/manifest.json", "unity/Packages/packages-lock.json", "unity/ProjectSettings/ProjectSettings.asset", "blender/source/Bird.blend"]:
    if subprocess.run(["git", "check-ignore", "-q", "--no-index", tracked], cwd=ROOT).returncode == 0:
        errors.append(f"Source accidentally ignored: {tracked}")
if errors:
    raise SystemExit("FAIL:\n" + "\n".join(errors))
print(f"PASS: {len(files)} files inspected; Python/JSON syntax, Unity metadata, local docs links, ignore rules")
