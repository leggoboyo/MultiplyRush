#!/usr/bin/env python3
"""
Simple repository storage guard.

- Warns for tracked files over WARN_BYTES
- Fails for tracked files over FAIL_BYTES
"""

from __future__ import annotations

import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
WARN_BYTES = 10 * 1024 * 1024   # 10 MiB
FAIL_BYTES = 25 * 1024 * 1024   # 25 MiB


def human_size(size: int) -> str:
    units = ["B", "KiB", "MiB", "GiB"]
    value = float(size)
    for unit in units:
        if value < 1024.0 or unit == units[-1]:
            return f"{value:.2f} {unit}"
        value /= 1024.0
    return f"{size} B"


def tracked_files() -> list[Path]:
    proc = subprocess.run(
        ["git", "ls-files", "-z"],
        cwd=ROOT,
        check=True,
        capture_output=True,
    )
    raw = proc.stdout.decode("utf-8", errors="replace")
    return [ROOT / p for p in raw.split("\0") if p]


def main() -> int:
    warnings: list[tuple[int, str]] = []
    failures: list[tuple[int, str]] = []

    for path in tracked_files():
        if not path.exists() or not path.is_file():
            continue
        size = path.stat().st_size
        rel = path.relative_to(ROOT).as_posix()
        if size >= FAIL_BYTES:
            failures.append((size, rel))
        elif size >= WARN_BYTES:
            warnings.append((size, rel))

    warnings.sort(reverse=True)
    failures.sort(reverse=True)

    for size, rel in warnings:
        print(f"[WARN] {rel} -> {human_size(size)}")

    for size, rel in failures:
        print(f"[FAIL] {rel} -> {human_size(size)} (exceeds {human_size(FAIL_BYTES)})")

    if failures:
        print("\nRepository size guard failed.")
        return 1

    print("Repository size guard passed.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
