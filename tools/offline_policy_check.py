#!/usr/bin/env python3
"""
Repository-level offline policy guard for Multiply Rush.

Fails if:
- banned packages (ads/analytics/IAP/cloud SDKs) exist in manifest.json
- risky player/connect settings are enabled
- gameplay/runtime scripts contain obvious network/ads/analytics API usage
"""

from __future__ import annotations

import json
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
PROJECT = ROOT / "My project"
MANIFEST = PROJECT / "Packages" / "manifest.json"
PROJECT_SETTINGS = PROJECT / "ProjectSettings" / "ProjectSettings.asset"
UNITY_CONNECT = PROJECT / "ProjectSettings" / "UnityConnectSettings.asset"
RUNTIME_SCRIPTS = PROJECT / "Assets" / "MultiplyRush" / "Scripts"

BANNED_PACKAGES = {
    "com.unity.ads",
    "com.unity.services.core",
    "com.unity.services.analytics",
    "com.unity.purchasing",
    "com.unity.remote-config",
    "com.google.firebase",
    "com.unity.multiplayer.center",
}

FORBIDDEN_API_PATTERNS = [
    r"\bUnityWebRequest\b",
    r"\bHttpClient\b",
    r"\bSystem\.Net\b",
    r"\bWebSocket\b",
    r"\bSocket\b",
    r"\bAdvertisement\b",
    r"\bPurchasing\b",
    r"\bAnalyticsService\b",
    r"\bRemoteConfig\b",
    r"\bFirebase\b",
]


def fail(msg: str) -> None:
    print(f"[FAIL] {msg}")


def ok(msg: str) -> None:
    print(f"[PASS] {msg}")


def load_text(path: Path) -> str:
    if not path.exists():
        raise FileNotFoundError(path)
    return path.read_text(encoding="utf-8")


def read_manifest_dependencies() -> set[str]:
    data = json.loads(load_text(MANIFEST))
    deps = data.get("dependencies", {})
    if not isinstance(deps, dict):
        return set()
    return set(deps.keys())


def yaml_value_is_zero(yaml_text: str, key: str) -> bool:
    pattern = re.compile(rf"(?m)^\s*{re.escape(key)}:\s*0\s*$")
    return bool(pattern.search(yaml_text))


def scan_runtime_scripts() -> list[str]:
    if not RUNTIME_SCRIPTS.exists():
        return [f"Missing scripts folder: {RUNTIME_SCRIPTS}"]

    findings: list[str] = []
    compiled_patterns = [(pat, re.compile(pat)) for pat in FORBIDDEN_API_PATTERNS]

    for cs_file in RUNTIME_SCRIPTS.rglob("*.cs"):
        # Allow editor-only scripts in runtime tree, if any
        if "/Editor/" in cs_file.as_posix():
            continue

        text = cs_file.read_text(encoding="utf-8", errors="ignore")
        for pat_src, pattern in compiled_patterns:
            if pattern.search(text):
                rel = cs_file.relative_to(ROOT).as_posix()
                findings.append(f"{rel} -> {pat_src}")
                break

    return findings


def main() -> int:
    failed = False

    try:
        dependencies = read_manifest_dependencies()
    except Exception as exc:
        fail(f"Could not read manifest: {exc}")
        return 1

    banned_found = sorted(BANNED_PACKAGES.intersection(dependencies))
    if banned_found:
        failed = True
        fail("Banned packages found: " + ", ".join(banned_found))
    else:
        ok("No banned ads/analytics/IAP/cloud packages in manifest.json")

    try:
        project_settings_text = load_text(PROJECT_SETTINGS)
    except Exception as exc:
        fail(f"Could not read ProjectSettings.asset: {exc}")
        return 1

    project_rules = {
        "submitAnalytics": "Unity analytics submission must remain disabled",
        "ForceInternetPermission": "Android INTERNET permission must not be forced",
        "uIRequiresPersistentWiFi": "iOS persistent Wi-Fi requirement must be disabled",
    }

    for key, description in project_rules.items():
        if yaml_value_is_zero(project_settings_text, key):
            ok(f"{key}=0 ({description})")
        else:
            failed = True
            fail(f"{key} is not 0 ({description})")

    try:
        unity_connect_text = load_text(UNITY_CONNECT)
    except Exception as exc:
        fail(f"Could not read UnityConnectSettings.asset: {exc}")
        return 1

    connect_rules = {
        "m_EngineDiagnosticsEnabled": "Engine diagnostics upload must be disabled",
        "m_InitializeOnStartup": "Unity services should not auto-initialize",
    }

    for key, description in connect_rules.items():
        # m_InitializeOnStartup can appear multiple times; require no enabled value.
        if key == "m_InitializeOnStartup":
            has_enabled = bool(re.search(r"(?m)^\s*m_InitializeOnStartup:\s*1\s*$", unity_connect_text))
            if has_enabled:
                failed = True
                fail(f"{key} has enabled entries ({description})")
            else:
                ok(f"{key}=0 for all service entries ({description})")
            continue

        if yaml_value_is_zero(unity_connect_text, key):
            ok(f"{key}=0 ({description})")
        else:
            failed = True
            fail(f"{key} is not 0 ({description})")

    has_enabled_service = bool(re.search(r"(?m)^\s*m_Enabled:\s*1\s*$", unity_connect_text))
    if has_enabled_service:
        failed = True
        fail("One or more Unity Connect services are enabled (m_Enabled: 1)")
    else:
        ok("All Unity Connect services disabled (m_Enabled: 0)")

    script_findings = scan_runtime_scripts()
    if script_findings:
        failed = True
        fail("Forbidden runtime API usage found:")
        for finding in script_findings[:30]:
            print(f"  - {finding}")
        if len(script_findings) > 30:
            print(f"  ... and {len(script_findings) - 30} more")
    else:
        ok("No forbidden network/ads/analytics API usage in runtime scripts")

    if failed:
        print("\nOffline policy check failed.")
        return 1

    print("\nOffline policy check passed.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
