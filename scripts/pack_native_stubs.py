#!/usr/bin/env python3
"""Pack placeholder nupkgs for native/*/specs/Release.nuspec into a local feed.

Every native package here only ships runtimes/**/native/*.dll files (no managed
assembly), so restore/build never inspects their contents. This lets CI (or any
workflow that never calls into the native code, e.g. unit tests built on fakes)
satisfy those PackageReferences without running the C++ build pipeline.
"""

import subprocess
import sys
import tempfile
import xml.etree.ElementTree as ET
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[1]


def make_stub_nuspec(real_nuspec: Path, placeholder: Path, out_path: Path) -> None:
    tree = ET.parse(real_nuspec)
    root = tree.getroot()
    files = root.find("files")
    if files is None:
        raise ValueError(f"{real_nuspec} has no <files> section")
    for file_elem in files.findall("file"):
        file_elem.set("src", str(placeholder))
    tree.write(out_path, xml_declaration=True, encoding="utf-8")


def main() -> int:
    if len(sys.argv) != 2:
        print("Usage: python pack_native_stubs.py <feed_dir>", file=sys.stderr)
        return 1

    feed_dir = Path(sys.argv[1])
    feed_dir.mkdir(parents=True, exist_ok=True)

    nuspecs = sorted(REPO_ROOT.glob("native/*/specs/Release.nuspec"))
    if not nuspecs:
        print("No native/*/specs/Release.nuspec files found", file=sys.stderr)
        return 1

    with tempfile.TemporaryDirectory() as tmp:
        tmp_dir = Path(tmp)
        placeholder = tmp_dir / "placeholder.dll"
        placeholder.write_bytes(b"stub")

        for nuspec in nuspecs:
            project_dir = nuspec.parents[1]
            project_csproj = project_dir / "project.csproj"
            stub_nuspec = tmp_dir / f"{project_dir.name}.nuspec"
            make_stub_nuspec(nuspec, placeholder, stub_nuspec)

            print(f"Packing stub for {project_dir.name}...")
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
