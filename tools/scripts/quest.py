#!/usr/bin/env python3
"""Connect to a Quest over Wi-Fi and install/launch the latest development build."""
import argparse
import ipaddress
import os
import re
import socket
import subprocess
import sys
import time
import zipfile
from concurrent.futures import ThreadPoolExecutor
from pathlib import Path
from tool_discovery import discover_adb, discover_unity

ROOT = Path(__file__).resolve().parents[2]
APK = ROOT / "builds" / "quest" / "VoarVR.apk"
PACKAGE = "com.voarvr.prototype"
QUEST_PRODUCT_DEVICE = "eureka"  # Quest 3's ro.product.device codename.
ADB_PORT = 5555
CACHE_FILE = ROOT / "artifacts" / "quest_ip.txt"
# Unity's freshly compiled IL2CPP runtime, which Gradle should have packaged into the APK.
IL2CPP_OUTPUT = ROOT / "unity" / "Library" / "Bee" / "Android" / "Prj" / "IL2CPP" / "Gradle" / "unityLibrary" / "src" / "main" / "jniLibs" / "arm64-v8a" / "libil2cpp.so"
GNU_BUILD_ID_NOTE = b"\x04\x00\x00\x00\x14\x00\x00\x00\x03\x00\x00\x00GNU\x00"
SCAN_WORKERS = 64
SCAN_TIMEOUT = 1.0  # Allow ARP resolution and Wi-Fi latency on an uncached address.


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


def is_wireless(serial):
    return ":" in serial or "._adb-tls-connect." in serial


def usb_devices(adb):
    return [serial for serial in list_devices(adb) if not is_wireless(serial)]


def current_device(adb):
    # Prefer USB while plugged in (faster installs); the cached Wi-Fi session remains afterwards.
    devices = sorted(list_devices(adb), key=is_wireless)
    if not devices:
        sys.exit(
            "No authorized adb device connected. Automatic discovery requires reachable wireless "
            "debugging; an IP override cannot enable it. Connect USB and accept the headset's "
            "debugging prompt if the wireless session needs to be enabled again."
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


def device_wifi_ip(adb, serial):
    result = subprocess.run([adb, "-s", serial, "shell", "ip", "-f", "inet", "addr", "show", "wlan0"],
                            capture_output=True, text=True, timeout=10)
    match = re.search(r"inet (\d+\.\d+\.\d+\.\d+)/", result.stdout)
    return match.group(1) if match else None


def enable_wireless(adb, serial):
    """From a USB session: open TCP 5555, connect over Wi-Fi and remember the address."""
    ip = device_wifi_ip(adb, serial)
    if not ip:
        print("Quest is on USB but has no Wi-Fi address; join Wi-Fi to use it unplugged.", flush=True)
        return None
    if not is_quest(adb, ip):
        # Restarting adbd briefly drops USB, so only do it when Wi-Fi debugging is off.
        run(adb, "-s", serial, "tcpip", str(ADB_PORT))
        for _ in range(15):
            time.sleep(1)
            if is_quest(adb, ip):
                break
        else:
            print(f"Enabled TCP {ADB_PORT} but could not connect to {ip}; check the Mac and Quest share a network.", flush=True)
            return None
    write_cached_ip(ip)
    print(f"Wireless debugging ready at {ip}:{ADB_PORT} (cached). You can unplug USB.", flush=True)
    return ip


def local_network():
    """Use the active interface's real IPv4 mask, including wider mesh LANs."""
    route = subprocess.run(["route", "-n", "get", "default"], capture_output=True, text=True)
    active = re.search(r"interface:\s*(\S+)", route.stdout)
    interfaces = list(dict.fromkeys(([active.group(1)] if active else []) + ["en0", "en1"]))
    for iface in interfaces:
        result = subprocess.run(["ifconfig", iface], capture_output=True, text=True)
        match = re.search(r"inet (\d+\.\d+\.\d+\.\d+) netmask (0x[0-9a-f]+)", result.stdout)
        if match:
            mask = str(ipaddress.IPv4Address(int(match.group(2), 16)))
            return ipaddress.ip_interface(f"{match.group(1)}/{mask}").network
    return None


def port_open(ip):
    try:
        with socket.create_connection((str(ip), ADB_PORT), timeout=SCAN_TIMEOUT):
            return str(ip)
    except OSError:
        return None


def fast_scan_for_quest(adb):
    """Concurrent TCP discovery across the actual local subnet, without nmap."""
    network = local_network()
    if network is None:
        print("Could not determine a local IPv4 network for discovery.", flush=True)
        return None
    if network.num_addresses > 4096:
        print(f"Network {network} is too large for automatic scanning; use QUEST_IP.", flush=True)
        return None
    hosts = [h for h in network.hosts()]
    print(f"Scanning {network} for Quest wireless debugging (TCP {ADB_PORT}, {len(hosts)} hosts)...", flush=True)
    with ThreadPoolExecutor(max_workers=SCAN_WORKERS) as pool:
        open_ips = [ip for ip in pool.map(port_open, hosts) if ip]
    for ip in open_ips:
        if is_quest(adb, ip):
            return ip
    if not open_ips:
        print(f"No TCP {ADB_PORT} listener found on {network}. Wi-Fi presence alone does not expose ADB.\n"
              "Wake the headset. If wireless debugging is off, connect authorized USB and run\n"
              "`adb tcpip 5555` with the SDK adb, then retry make quest-connect.", flush=True)
    return None


def connect(adb, quest_ip):
    if quest_ip:
        run(adb, "connect", f"{quest_ip}:{ADB_PORT}")
        if list_devices(adb):
            write_cached_ip(quest_ip)
        current_device(adb)
        return

    devices = list_devices(adb)
    if any(is_wireless(serial) for serial in devices):
        return  # a wireless session is already live
    usb = [serial for serial in devices if not is_wireless(serial)]
    if usb:
        enable_wireless(adb, usb[0])
        current_device(adb)
        return

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


def elf_build_id(data):
    at = data.find(GNU_BUILD_ID_NOTE)
    return data[at + len(GNU_BUILD_ID_NOTE):at + len(GNU_BUILD_ID_NOTE) + 20].hex() if at >= 0 else None


def check_il2cpp_matches(apk=APK, compiled=IL2CPP_OUTPUT):
    """A stale Gradle merge once packaged an older libil2cpp.so beside new metadata,
    which crashes in il2cpp_init before any game code runs."""
    if not compiled.is_file():
        return  # No local build cache to compare (e.g. an APK copied from elsewhere).
    with zipfile.ZipFile(apk) as archive:
        packaged = elf_build_id(archive.read("lib/arm64-v8a/libil2cpp.so"))
    expected = elf_build_id(compiled.read_bytes())
    if packaged and expected and packaged != expected:
        sys.exit(f"{apk.name} packages libil2cpp {packaged[:12]}… but Unity compiled {expected[:12]}…; "
                 "it would crash on launch. Delete unity/Library/Bee/Android/Prj/IL2CPP/Gradle/*/build and rebuild.")


def install(adb, quest_ip):
    if not APK.is_file():
        sys.exit(f"No build found at {APK}. Run: python3 tools/scripts/unity.py build-quest")
    check_il2cpp_matches()
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
