#!/usr/bin/env python3
"""Run the pinned Unity project without depending on a fixed Mac installation path."""
import argparse
import os
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[2]


def check_results(path):
    root = ET.parse(path).getroot()
    cases = list(root.iter("test-case"))
    if not cases or root.get("result") not in {"Passed", "Success"}:
        raise ValueError("Unity tests did not pass or produced zero test cases.")
    if any(case.get("result") not in {"Passed", "Success"} for case in cases):
        raise ValueError("Unity reported failed, skipped or inconclusive tests.")
    return len(cases)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("command", choices=["configure", "test-edit", "test-play", "build-quest"])
    args = parser.parse_args()
    editor = os.environ.get("UNITY_EDITOR") or shutil.which("Unity")
    if not editor or not Path(editor).is_file() or not os.access(editor, os.X_OK):
        parser.error("Set UNITY_EDITOR to the executable inside your installed Unity.app (see README).")
    artifacts = ROOT / "artifacts" / "unity"
    artifacts.mkdir(parents=True, exist_ok=True)
    run_dir = Path(tempfile.mkdtemp(prefix=args.command + "-", dir=artifacts))
    command = [editor, "-batchmode", "-projectPath", str(ROOT / "unity"), "-logFile", str(run_dir / "editor.log")]
    if args.command.startswith("test-"):
        results = run_dir / "results.xml"
        command += ["-nographics", "-runTests", "-testPlatform", "EditMode" if args.command == "test-edit" else "PlayMode",
                    "-testResults", str(results)]
        # No -quit: the Unity test runner exits after writing results.
    else:
        if args.command == "build-quest":
            command += ["-buildTarget", "Android"]
        command += ["-quit", "-executeMethod", "VoarVR.Editor.ProjectSetup." + ("Configure" if args.command == "configure" else "BuildQuest")]
    print("Unity log:", run_dir / "editor.log", flush=True)
    result = subprocess.run(command, cwd=ROOT / "unity", check=False)
    if result.returncode:
        return result.returncode
    if args.command.startswith("test-"):
        try:
            print(f"PASS: {check_results(results)} Unity tests")
        except (OSError, ET.ParseError, ValueError) as error:
            print(f"FAIL: {error}", file=sys.stderr)
            return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
