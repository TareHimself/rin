#!/usr/bin/env python3
"""Print, as a JSON list, the .csproj files a set of changed files can affect.

CI runs a test job only when its project is in the list. A project is affected when a changed file is inside
it, or inside any project, native module or native fake it depends on (following ProjectReference, the
TareHimself.<name> native packages, and the Rin.Shade compile step for projects that import RinShade.targets).
Markdown files never affect anything. Any other file outside a project or native module (build props, CI,
scripts) affects everything.

Usage: python scripts/ci_affected_projects.py <base-ref> [head-ref]
"""

import json
import re
import subprocess
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
SKIPPED_DIRS = {"bin", "obj", ".git", ".venv", "node_modules", ".feed"}
SHADE_COMPILER = ROOT / "Shade/Rin.Shade.MSBuild/Rin.Shade.MSBuild.csproj"


def directory_properties() -> dict[str, Path]:
    props = (ROOT / "Directory.Build.props").read_text(encoding="utf-8")
    return {name: ROOT / path.replace("\\", "/")
            for name, path in re.findall(r"<(\w+Dir)>\$\(RepoRoot\)([^<]*)</\1>", props)}


def all_projects() -> list[Path]:
    return [p for p in ROOT.rglob("*.csproj") if not SKIPPED_DIRS.intersection(p.relative_to(ROOT).parts)]


def dependencies(project: Path, dirs: dict[str, Path]) -> tuple[list[Path], list[Path]]:
    """The projects and native module folders this project depends on directly."""
    projects, native = [], []
    for element in ET.parse(project).iter():
        include = element.get("Include") or element.get("Project") or ""
        tag = element.tag.split("}")[-1]
        if tag == "ProjectReference":
            path = include.replace("\\", "/")
            for name, value in dirs.items():
                path = path.replace(f"$({name})", f"{value.as_posix()}/")
            projects.append((project.parent / path).resolve())
        elif tag == "Import" and include.endswith("RinShade.targets"):
            projects.append(SHADE_COMPILER)
        elif tag == "PackageReference" and include.endswith(".Native"):
            module = include.removeprefix("TareHimself.")
            native += [ROOT / "native" / module, ROOT / "native/Fakes" / f"{module}.Fake"]
    return projects, native


def changed_files(base: str, head: str) -> list[Path]:
    names = subprocess.run(["git", "diff", "--name-only", f"{base}...{head}"], cwd=ROOT, check=True,
                           capture_output=True, text=True).stdout.split()
    return [ROOT / name for name in names if not name.endswith(".md")]


def main() -> None:
    dirs = directory_properties()
    projects = all_projects()
    direct = {p: dependencies(p, dirs) for p in projects}

    def closure(project: Path, seen: set[Path]) -> set[Path]:
        """Every folder whose changes affect the project: its own, its dependencies' and their native modules."""
        if project in seen or project not in direct:
            return set()
        seen.add(project)
        refs, native = direct[project]
        folders = {project.parent, *native}
        for ref in refs:
            folders |= closure(ref, seen)
        return folders

    # Native modules (they have a silopkg.py) and fakes own their folders. Anything else under native/, like
    # recipes and silo.config.toml, can change every native build, so it falls through to "affects everything".
    owners = [p.parent for p in projects] + [d.parent for d in (ROOT / "native").glob("*/silopkg.py")]
    changed_owners = set()
    for file in changed_files(sys.argv[1], sys.argv[2] if len(sys.argv) > 2 else "HEAD"):
        owner = max((o for o in owners if o in file.parents), key=lambda o: len(o.parts), default=None)
        if owner is None:
            print(json.dumps(sorted(p.relative_to(ROOT).as_posix() for p in projects)))
            return
        changed_owners.add(owner)

    affected = [p for p in projects if closure(p, set()) & changed_owners]
    print(json.dumps(sorted(p.relative_to(ROOT).as_posix() for p in affected)))


if __name__ == "__main__":
    main()
