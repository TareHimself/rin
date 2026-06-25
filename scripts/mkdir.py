#!/usr/bin/env python3
"""Create a directory recursively from a single argument."""

import sys
from pathlib import Path


def main() -> int:
    if len(sys.argv) != 2:
        print("Usage: python mkdir.py <directory>", file=sys.stderr)
        return 1

    target = Path(sys.argv[1])
    if not target:
        print("Error: directory path must not be empty", file=sys.stderr)
        return 1

    try:
        target.mkdir(parents=True, exist_ok=True)
    except Exception as exc:
        print(f"Failed to create directory '{target}': {exc}", file=sys.stderr)
        return 2

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
