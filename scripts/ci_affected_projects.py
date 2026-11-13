#!/usr/bin/env python3
"""Print, as a JSON list, the .csproj files a set of changed files can affect.

CI runs a job only when its project is in the list. A project is affected when a changed file is inside
it, or inside any project, native module or native fake it depends on (following ProjectReference, the
TareHimself.<name> native packages, and the Rin.Shade compile step for projects that import RinShade.targets),
or is a file or folder outside the project that it or a dependency includes (a linked Compile item, for example).
Markdown files never affect anything. Any other file outside a project or native module (build props, CI,
scripts) affects everything.

Usage: python scripts/ci_affected_projects.py <base-ref> [head-ref]
"""

import json
import re
import subprocess
import sys
import xml.etree.ElementTree as ET
from dataclasses import dataclass, field
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
SKIPPED_DIRS = {"bin", "obj", ".git", ".venv", "node_modules", ".feed"}
SHADE_COMPILER = ROOT / "Shade/Rin.Shade.MSBuild/Rin.Shade.MSBuild.csproj"
FILE_ITEMS = {"Compile", "Content", "None", "EmbeddedResource", "AdditionalFiles"}


@dataclass
class Dependencies:
    """What one project depends on directly."""

    projects: list[Path] = field(default_factory=list)
    native: list[Path] = field(default_factory=list)
    linked: list[Path] = field(default_factory=list)


def directory_properties() -> dict[str, Path]:
    props = (ROOT / "Directory.Build.props").read_text(encoding="utf-8")
    return {name: ROOT / path.replace("\\", "/")
            for name, path in re.findall(r"<(\w+Dir)>\$\(RepoRoot\)([^<]*)</\1>", props)}


def all_projects() -> list[Path]:
    return [p for p in ROOT.rglob("*.csproj") if not SKIPPED_DIRS.intersection(p.relative_to(ROOT).parts)]


def resolve_include(project: Path, include: str, dirs: dict[str, Path]) -> Path | None:
    """The path an Include points at, cut at its first wildcard. None when it uses an unknown property."""
    path = include.replace("\\", "/")
    for name, value in dirs.items():
        path = path.replace(f"$({name})", f"{value.as_posix()}/")
    if "$(" in path:
        return None
    fixed = []
    for part in path.split("/"):
        if "*" in part:
            break
        fixed.append(part)
    return (project.parent / "/".join(fixed)).resolve()


def dependencies(project: Path, dirs: dict[str, Path]) -> Dependencies:
    result = Dependencies()
    for element in ET.parse(project).iter():
        include = element.get("Include") or element.get("Project") or ""
        tag = element.tag.split("}")[-1]
        if tag == "ProjectReference":
            if (path := resolve_include(project, include, dirs)) is not None:
                result.projects.append(path)
        elif tag == "Import" and include.endswith("RinShade.targets"):
            result.projects.append(SHADE_COMPILER)
        elif tag == "PackageReference" and include.endswith(".Native"):
            module = include.removeprefix("TareHimself.")
            result.native += [ROOT / "native" / module, ROOT / "native/Fakes" / f"{module}.Fake"]
        elif tag in FILE_ITEMS and include:
            path = resolve_include(project, include, dirs)
            if path is not None and project.parent not in (path, *path.parents):
                result.linked.append(path)
    return result


def changed_files(base: str, head: str) -> list[Path]:
    # -z keeps paths with spaces or non-ASCII characters whole and unquoted.
    names = subprocess.run(["git", "diff", "--name-only", "-z", f"{base}...{head}"], cwd=ROOT, check=True,
                           capture_output=True, text=True, encoding="utf-8").stdout.split("\0")
    return [ROOT / name for name in names if name and not name.endswith(".md")]


def main() -> None:
    dirs = directory_properties()
    projects = all_projects()
    direct = {p: dependencies(p, dirs) for p in projects}

    def reachable(project: Path, seen: set[Path]) -> set[Path]:
        """The project and everything it depends on, transitively."""
        if project in seen or project not in direct:
            return seen
        seen.add(project)
        for ref in direct[project].projects:
            reachable(ref, seen)
        return seen

    # Native modules (they have a silopkg.py) and fakes own their folders. Anything else under native/, like
    # recipes and silo.config.toml, can change every native build, so it falls through to "affects everything".
    owners = [p.parent for p in projects] + [d.parent for d in (ROOT / "native").glob("*/silopkg.py")]
    changed = changed_files(sys.argv[1], sys.argv[2] if len(sys.argv) > 2 else "HEAD")
    changed_owners = set()
    for file in changed:
        owner = max((o for o in owners if o in file.parents), key=lambda o: len(o.parts), default=None)
        if owner is None:
            print(json.dumps(sorted(p.relative_to(ROOT).as_posix() for p in projects)))
            return
        changed_owners.add(owner)

    def is_affected(project: Path) -> bool:
        closure = reachable(project, set())
        folders = {p.parent for p in closure} | {folder for p in closure for folder in direct[p].native}
        if folders & changed_owners:
            return True
        linked = {path for p in closure for path in direct[p].linked}
        return any(file in linked or linked.intersection(file.parents) for file in changed)

    affected = [p for p in projects if is_affected(p)]
    print(json.dumps(sorted(p.relative_to(ROOT).as_posix() for p in affected)))


if __name__ == "__main__":
    main()
