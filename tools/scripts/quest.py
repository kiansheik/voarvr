#!/usr/bin/env python3
"""Connect to a Quest over Wi-Fi and install/launch the latest development build."""
import argparse
import subprocess
import sys
from pathlib import Path
from tool_discovery import discover_adb, discover_unity

ROOT = Path(__file__).resolve().parents[2]
APK = ROOT / "builds" / "quest" / "VoarVR.apk"
PACKAGE = "com.voarvr.prototype"


def require_adb():
    found = discover_adb(discover_unity(ROOT))
    if found.status != "FOUND":
        sys.exit(f"{found.status}: {found.detail}")
    return str(found.path)


def run(adb, *args):
    print("$ adb", *args, flush=True)
    subprocess.run([adb, *args], check=True)


def current_device(adb):
    result = subprocess.run([adb, "devices"], check=True, capture_output=True, text=True)
    lines = [line.split("\t") for line in result.stdout.strip().splitlines()[1:] if line.strip()]
    devices = [serial for serial, state in lines if state == "device"]
    if not devices:
        sys.exit(
            "No adb device connected. Set QUEST_IP=<headset ip> to connect over Wi-Fi, "
            "or plug the Quest in via USB once."
        )
    return devices[0]


def connect(adb, quest_ip):
    if quest_ip:
        run(adb, "connect", f"{quest_ip}:5555")
    current_device(adb)  # fail fast with a clear message if nothing is reachable yet


def install(adb, quest_ip):
    if not APK.is_file():
        sys.exit(f"No build found at {APK}. Run: python3 tools/scripts/unity.py build-quest")
    connect(adb, quest_ip)
    run(adb, "-s", current_device(adb), "install", "-r", str(APK))


def launch(adb, quest_ip):
    connect(adb, quest_ip)
    run(adb, "-s", current_device(adb), "shell", "monkey", "-p", PACKAGE,
        "-c", "android.intent.category.LAUNCHER", "1")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("command", choices=["connect", "install", "launch", "run"], default="run", nargs="?")
    parser.add_argument("--quest-ip", default=None, help="Headset Wi-Fi IP (defaults to $QUEST_IP env var).")
    args = parser.parse_args()
    import os
    quest_ip = args.quest_ip or os.environ.get("QUEST_IP")
    adb = require_adb()
    if args.command == "connect":
        connect(adb, quest_ip)
    elif args.command == "install":
        install(adb, quest_ip)
    elif args.command == "launch":
        launch(adb, quest_ip)
    else:
        install(adb, quest_ip)
        launch(adb, quest_ip)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
