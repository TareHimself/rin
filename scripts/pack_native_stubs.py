#!/usr/bin/env python3
"""Pack placeholder nupkgs for the native/*.Native packages into a local feed.

Every native package here only ships runtimes/**/native/*.dll files (no managed
assembly), so restore/build never inspects their contents. This lets CI (or any
workflow that never calls into the native code, e.g. unit tests built on fakes)
satisfy those PackageReferences without running the C++ build pipeline (Conan/
CMake/Vulkan SDK) or even the real nuspec generator, which needs already-built
binaries to scan and so can't run on a clean checkout either.

Package ids/versions come from the repo's own Directory.Packages.props (the
single source of truth other projects already reference), not duplicated here.
"""

import re
import subprocess
import sys
import tempfile
import xml.etree.ElementTree as ET
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[1]


def resolve_version(pattern: str) -> str:
    parts = [p if p != "*" else "0" for p in pattern.split(".")]
    while len(parts) < 3:
        parts.append("0")
    return ".".join(parts)


def native_packages() -> list[tuple[str, str]]:
    tree = ET.parse(REPO_ROOT / "Directory.Packages.props")
    result = []
    for elem in tree.getroot().iter("PackageVersion"):
        package_id = elem.get("Include", "")
        if package_id.endswith(".Native"):
            result.append((package_id, resolve_version(elem.get("Version", "1.0.0"))))
    return result


def make_stub_nuspec(package_id: str, version: str, placeholder: Path, out_path: Path) -> None:
    nuspec = f"""<?xml version="1.0" encoding="utf-8"?>
<package>
  <metadata>
    <id>{package_id}</id>
    <version>{version}</version>
    <authors>TareHimself</authors>
    <description>Stub native package (no real binaries) for CI/tests that never call into it.</description>
    <packageTypes>
      <packageType name="Dependency" />
    </packageTypes>
  </metadata>
  <files>
    <file src="{placeholder}" target="runtimes/win-x64/native/stub.dll" />
  </files>
</package>
"""
    out_path.write_text(nuspec, encoding="utf-8")


def main() -> int:
    if len(sys.argv) != 2:
        print("Usage: python pack_native_stubs.py <feed_dir>", file=sys.stderr)
        return 1

    feed_dir = Path(sys.argv[1])
    feed_dir.mkdir(parents=True, exist_ok=True)

    packages = native_packages()
    if not packages:
        print("No *.Native packages found in Directory.Packages.props", file=sys.stderr)
        return 1

    with tempfile.TemporaryDirectory() as tmp:
        tmp_dir = Path(tmp)
        placeholder = tmp_dir / "placeholder.dll"
        placeholder.write_bytes(b"stub")

        # project dir name = package id with the "TareHimself." prefix stripped
        for package_id, version in packages:
            project_name = re.sub(r"^TareHimself\.", "", package_id)
            project_csproj = REPO_ROOT / "native" / project_name / "project.csproj"
            if not project_csproj.exists():
                print(f"Skipping {package_id}: no {project_csproj}", file=sys.stderr)
                continue

            stub_nuspec = tmp_dir / f"{project_name}.nuspec"
            make_stub_nuspec(package_id, version, placeholder, stub_nuspec)

            print(f"Packing stub for {package_id} {version}...")
            subprocess.run(
                [
                    "dotnet", "pack", str(project_csproj),
                    f"/p:NuspecFile={stub_nuspec}",
                    "--output", str(feed_dir),
                ],
                check=True,
            )

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
