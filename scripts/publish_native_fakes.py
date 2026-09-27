#!/usr/bin/env python3
"""Publish the NativeAOT fake-native libraries under native/Fakes/*.

Each fake is plain C# compiled to a real native library via NativeAOT, standing in
for a real native/*.Native library so tests that call into it (Rin.GLTF.Tests,
Rin.Slang.Compiler.Tests) can run without the real C++ build (Conan/CMake/Vulkan SDK).
See scripts/pack_native_stubs.py for the separate restore-only stub packages.
"""

import subprocess
import sys
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[1]


def main() -> int:
    rid = sys.argv[1] if len(sys.argv) > 1 else "win-x64"

    fakes_dir = REPO_ROOT / "native" / "Fakes"
    csprojs = sorted(fakes_dir.glob("*/*.csproj"))
    if not csprojs:
        print(f"No fake projects found under {fakes_dir}", file=sys.stderr)
        return 1

    for csproj in csprojs:
        print(f"Publishing {csproj.parent.name} ({rid})...")
        subprocess.run(
            [
                "dotnet", "publish", str(csproj),
                "-c", "Release",
                "-r", rid,
                "--self-contained",
            ],
            check=True,
        )

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
