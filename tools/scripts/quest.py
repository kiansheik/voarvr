#!/usr/bin/env python3
"""Connect to a Quest over Wi-Fi and install/launch the latest development build."""
import argparse
import ipaddress
import os
import re
import socket
import subprocess
import sys
from concurrent.futures import ThreadPoolExecutor
from pathlib import Path
from tool_discovery import discover_adb, discover_unity

ROOT = Path(__file__).resolve().parents[2]
APK = ROOT / "builds" / "quest" / "VoarVR.apk"
PACKAGE = "com.voarvr.prototype"
QUEST_PRODUCT_DEVICE = "eureka"  # Quest 3's ro.product.device codename.
ADB_PORT = 5555
CACHE_FILE = ROOT / "artifacts" / "quest_ip.txt"
SCAN_WORKERS = 128
SCAN_TIMEOUT = 0.3  # seconds per host; a dead/unused IP just times out silently.


def require_adb():
    found = discover_adb(discover_unity(ROOT))
    if found.status != "FOUND":
        sys.exit(f"{found.status}: {found.detail}")
    return str(found.path)


def run(adb, *args):
    print("$ adb", *args, flush=True)
    subprocess.run([adb, *args], check=True)


def list_devices(adb):
    result = subprocess.run([adb, "devices"], check=True, capture_output=True, text=True)
    lines = [line.split("\t") for line in result.stdout.strip().splitlines()[1:] if line.strip()]
    return [serial for serial, state in lines if state == "device"]


def current_device(adb):
    devices = list_devices(adb)
    if not devices:
        sys.exit(
            "No adb device connected. Set QUEST_IP=<headset ip>, or plug the Quest in via "
            "USB once to refresh its wireless adb session."
        )
    return devices[0]


def read_cached_ip():
    try:
        return CACHE_FILE.read_text().strip() or None
    except FileNotFoundError:
        return None


def write_cached_ip(ip):
    CACHE_FILE.parent.mkdir(parents=True, exist_ok=True)
    CACHE_FILE.write_text(ip + "\n")


def is_quest(adb, ip):
    """adb connect + verify it's actually a Quest, not just something with the port open."""
    subprocess.run([adb, "connect", f"{ip}:{ADB_PORT}"], capture_output=True, text=True)
    probe = subprocess.run(
        [adb, "-s", f"{ip}:{ADB_PORT}", "shell", "getprop", "ro.product.device"],
        capture_output=True, text=True, timeout=5,
    )
    if probe.returncode == 0 and probe.stdout.strip() == QUEST_PRODUCT_DEVICE:
        return True
    subprocess.run([adb, "disconnect", f"{ip}:{ADB_PORT}"], capture_output=True, text=True)
    return False


def local_network():
    """The /24 containing this Mac's Wi-Fi/Ethernet IP. A Deco (or similar) mesh still
    bridges every node onto one flat subnet with a single DHCP pool, so this covers the
    Quest regardless of which physical mesh node it's actually associated with."""
    for iface in ("en0", "en1"):
        result = subprocess.run(["ifconfig", iface], capture_output=True, text=True)
        match = re.search(r"inet (\d+\.\d+\.\d+\.\d+) netmask (0x[0-9a-f]+)", result.stdout)
        if match:
            return ipaddress.ip_interface(f"{match.group(1)}/24").network
    return None


def port_open(ip):
    try:
        with socket.create_connection((str(ip), ADB_PORT), timeout=SCAN_TIMEOUT):
            return str(ip)
    except OSError:
        return None


def fast_scan_for_quest(adb):
    """Pure-Python concurrent TCP connect scan of the local /24 for port 5555 (no nmap).
    ~256 hosts at once with a short per-connection timeout finishes in about a second."""
    network = local_network()
    if network is None:
        return None
    hosts = [h for h in network.hosts()]
    print(f"Scanning {network} for a Quest ({len(hosts)} hosts, no nmap)...", flush=True)
    with ThreadPoolExecutor(max_workers=SCAN_WORKERS) as pool:
        open_ips = [ip for ip in pool.map(port_open, hosts) if ip]
    for ip in open_ips:
        if is_quest(adb, ip):
            return ip
    return None


def connect(adb, quest_ip):
    if quest_ip:
        run(adb, "connect", f"{quest_ip}:{ADB_PORT}")
        if list_devices(adb):
            write_cached_ip(quest_ip)
        current_device(adb)
        return

    if list_devices(adb):
        return  # already connected (USB or a prior wireless session still live)

    cached = read_cached_ip()
    if cached and is_quest(adb, cached):
        print(f"Reconnected to cached Quest IP {cached}.", flush=True)
        current_device(adb)
        return

    found_ip = fast_scan_for_quest(adb)
    if found_ip:
        print(f"Found Quest at {found_ip}.", flush=True)
        write_cached_ip(found_ip)
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
