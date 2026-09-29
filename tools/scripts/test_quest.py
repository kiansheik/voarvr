import ipaddress
import subprocess
import unittest
from unittest.mock import patch

import quest


class QuestDiscoveryTests(unittest.TestCase):
    def test_mesh_subnet_includes_addresses_outside_first_24(self):
        responses = [
            subprocess.CompletedProcess([], 0, "interface: en0\n"),
            subprocess.CompletedProcess([], 0, "inet 192.168.68.54 netmask 0xfffffc00 broadcast 192.168.71.255\n"),
        ]
        with patch.object(quest.subprocess, "run", side_effect=responses):
            network = quest.local_network()
        self.assertEqual(network, ipaddress.ip_network("192.168.68.0/22"))
        self.assertIn(ipaddress.ip_address("192.168.71.100"), network)

    def test_active_interface_precedes_en0(self):
        responses = [
            subprocess.CompletedProcess([], 0, "interface: en7\n"),
            subprocess.CompletedProcess([], 0, "inet 10.0.0.30 netmask 0xffffff00\n"),
        ]
        with patch.object(quest.subprocess, "run", side_effect=responses) as run:
            self.assertEqual(quest.local_network(), ipaddress.ip_network("10.0.0.0/24"))
        self.assertEqual(run.call_args.args[0], ["ifconfig", "en7"])

    def test_no_default_route_uses_available_interface(self):
        responses = [
            subprocess.CompletedProcess([], 1, ""),
            subprocess.CompletedProcess([], 0, ""),
            subprocess.CompletedProcess([], 0, "inet 192.168.4.2 netmask 0xffffff00\n"),
        ]
        with patch.object(quest.subprocess, "run", side_effect=responses):
            self.assertEqual(quest.local_network(), ipaddress.ip_network("192.168.4.0/24"))

    def test_large_network_does_not_start_unbounded_scan(self):
        with patch.object(quest, "local_network", return_value=ipaddress.ip_network("10.0.0.0/8")), \
                patch.object(quest, "port_open") as probe, patch("builtins.print"):
            self.assertIsNone(quest.fast_scan_for_quest("adb"))
        probe.assert_not_called()


    def test_usb_connect_enables_wifi_and_caches_address(self):
        ip_output = "3: wlan0: <UP>\n    inet 192.168.68.52/22 brd 192.168.71.255 scope global wlan0\n"
        attempts = iter([False, True])
        with patch.object(quest, "list_devices", return_value=["2G0YC1ZG2G04GZ"]), \
                patch.object(quest.subprocess, "run", return_value=subprocess.CompletedProcess([], 0, ip_output)), \
                patch.object(quest, "is_quest", side_effect=lambda adb, ip: next(attempts)), \
                patch.object(quest, "run") as adb_run, patch.object(quest, "write_cached_ip") as cache, \
                patch.object(quest.time, "sleep"), patch("builtins.print"):
            quest.connect("adb", None)
        adb_run.assert_called_once_with("adb", "-s", "2G0YC1ZG2G04GZ", "tcpip", "5555")
        cache.assert_called_once_with("192.168.68.52")

    def test_usb_connect_skips_adbd_restart_when_wifi_already_open(self):
        with patch.object(quest, "list_devices", return_value=["2G0YC1ZG2G04GZ"]), \
                patch.object(quest, "device_wifi_ip", return_value="192.168.68.52"), \
                patch.object(quest, "is_quest", return_value=True), \
                patch.object(quest, "run") as adb_run, patch.object(quest, "write_cached_ip") as cache, \
                patch("builtins.print"):
            quest.connect("adb", None)
        adb_run.assert_not_called()
        cache.assert_called_once_with("192.168.68.52")

    def test_live_wireless_session_is_reused_and_preferred_after_usb(self):
        with patch.object(quest, "list_devices", return_value=["192.168.68.52:5555", "2G0YC1ZG2G04GZ"]), \
                patch.object(quest, "enable_wireless") as enable:
            quest.connect("adb", None)
            enable.assert_not_called()
            self.assertEqual(quest.current_device("adb"), "2G0YC1ZG2G04GZ")
        with patch.object(quest, "list_devices", return_value=["192.168.68.52:5555"]):
            self.assertEqual(quest.current_device("adb"), "192.168.68.52:5555")


    def test_install_refuses_apk_with_stale_il2cpp_runtime(self):
        import tempfile, zipfile
        from pathlib import Path
        note = quest.GNU_BUILD_ID_NOTE
        with tempfile.TemporaryDirectory() as folder:
            apk, compiled = Path(folder) / "app.apk", Path(folder) / "libil2cpp.so"
            with zipfile.ZipFile(apk, "w") as archive:
                archive.writestr("lib/arm64-v8a/libil2cpp.so", b"elf" + note + bytes(range(20)))
            compiled.write_bytes(b"elf" + note + bytes(range(20)))
            quest.check_il2cpp_matches(apk, compiled)
            compiled.write_bytes(b"elf" + note + bytes(range(1, 21)))
            with self.assertRaises(SystemExit):
                quest.check_il2cpp_matches(apk, compiled)


if __name__ == "__main__":
    unittest.main()
