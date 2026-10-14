#!/usr/bin/env python3
"""Build native modules with silo and pack each one into the local NuGet feed.

A module is a folder under native/ with a silopkg.py. silo builds it into its store, where
the module's bin folder holds the library and every shared library it loads. Those files
become runtimes/<rid>/native/ of a package named TareHimself.<module>.
"""

import argparse
import json
import shutil
import subprocess
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[1]
NATIVE_DIR = REPO_ROOT / "native"
AUTHOR = "TareHimself"
LIBRARY_PATTERNS = ("*.dll", "*.so", "*.so.*", "*.dylib")


def all_modules() -> list[str]:
    return sorted(recipe.parent.name for recipe in NATIVE_DIR.glob("*/silopkg.py"))


def dotnet_output(*args: str) -> str:
    result = subprocess.run(
        ["dotnet", *args], check=True, capture_output=True, encoding="utf-8", errors="replace"
    )
    return result.stdout


def labelled_value(output: str, label: str) -> str:
    for line in output.splitlines():
        key, separator, value = line.strip().partition(":")
        if separator and key == label:
            return value.strip()
    raise RuntimeError(f"dotnet did not print '{label}:'")


def build(module_dir: Path) -> dict:
    """Build the module into silo's store and return silo's report on it."""
    report_dir = module_dir / "build-report"
    result = subprocess.run(
        ["silo", "build", str(module_dir), "--report", str(report_dir)], check=False
    )
    report = json.loads((report_dir / "report.json").read_text(encoding="utf-8"))
    if result.returncode != 0 or not report["ok"]:
        raise RuntimeError(f"{report['error']}\nLogs are in {report_dir / 'logs'}")
    return report


def built_version(report: dict) -> str:
    return next(
        package["version"] for package in report["packages"] if package["name"] == report["root"]
    )


def write_nuspec(module: str, version: str, rid: str, binaries_dir: Path, nuspec: Path) -> None:
    """Sources are file names, resolved against NuspecBasePath when the package is packed."""
    package = ET.Element("package")
    metadata = ET.SubElement(package, "metadata")
    ET.SubElement(metadata, "id").text = f"{AUTHOR}.{module}"
    ET.SubElement(metadata, "Title").text = module
    ET.SubElement(metadata, "version").text = version
    ET.SubElement(metadata, "authors").text = AUTHOR
    ET.SubElement(metadata, "description").text = "Native binaries"
    package_types = ET.SubElement(metadata, "packageTypes")
    ET.SubElement(package_types, "packageType", {"name": "Dependency"})

    files = ET.SubElement(package, "files")
    libraries = sorted(
        {file.name for pattern in LIBRARY_PATTERNS for file in binaries_dir.glob(pattern)}
    )
    if not libraries:
        raise RuntimeError(f"{binaries_dir} holds no shared library to pack")
    for name in libraries:
        ET.SubElement(files, "file", {"src": name, "target": f"runtimes/{rid}/native/{name}"})

    ET.indent(package, space="  ")
    nuspec.parent.mkdir(parents=True, exist_ok=True)
    ET.ElementTree(package).write(nuspec, encoding="utf-8", xml_declaration=True)


def evict_from_nuget_cache(module: str, version: str) -> None:
    """Restore would otherwise keep using the copy it unpacked from an earlier build."""
    cache = Path(labelled_value(dotnet_output("nuget", "locals", "global-packages", "--list"),
                                "global-packages"))
    shutil.rmtree(cache / f"{AUTHOR}.{module}".lower() / version, ignore_errors=True)


def pack(module: str, feed_dir: Path, rid: str, version_override: str | None) -> None:
    module_dir = NATIVE_DIR / module
    report = build(module_dir)
    version = version_override or built_version(report)
    binaries_dir = Path(report["store_path"]) / "bin"
    nuspec = module_dir / "specs" / "Release.nuspec"
    write_nuspec(module, version, rid, binaries_dir, nuspec)
    subprocess.run(
        [
            "dotnet", "pack", str(module_dir / "project.csproj"),
            f"-p:NuspecFile={nuspec}",
            f"-p:NuspecBasePath={binaries_dir}",
            "--output", str(feed_dir),
        ],
        check=True,
    )
    evict_from_nuget_cache(module, version)


def main() -> int:
    modules = all_modules()
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("modules", nargs="*", metavar="module",
                        help=f"Modules to pack, all of them when left out: {', '.join(modules)}")
    parser.add_argument("--feed", type=Path, default=REPO_ROOT / ".feed", help="Feed folder")
    parser.add_argument("--version", help="Package version, in place of the recipe's")
    args = parser.parse_args()
    unknown = sorted(set(args.modules) - set(modules))
    if unknown:
        parser.error(f"no such module: {', '.join(unknown)} (there are {', '.join(modules)})")

    try:
        rid = labelled_value(dotnet_output("--info"), "RID")
        args.feed.mkdir(parents=True, exist_ok=True)
        for module in args.modules or modules:
            pack(module, args.feed.resolve(), rid, args.version)
    except (RuntimeError, subprocess.CalledProcessError) as error:
        print(f"error: {error}", file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
