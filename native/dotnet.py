import subprocess
from pathlib import Path
import xml.etree.ElementTree as ET
def get_dotnet_rid() -> str:
    try:
        result = subprocess.run(
            ["dotnet", "--info"],
            check=True,
            capture_output=True,
            text=True,
            encoding="utf-8",
            errors="replace",
        )
    except FileNotFoundError:
        raise RuntimeError("dotnet was not found on PATH")
    except subprocess.CalledProcessError as e:
        raise RuntimeError(f"dotnet --info failed:\n{e.stderr or e.stdout}")

    for line in result.stdout.splitlines():
        key, sep, value = line.strip().partition(":")
        if sep and key == "RID":
            return value.strip()

    raise RuntimeError("Could not find RID in `dotnet --info` output")

def make_nuspec(name: str,version: str,author: str,source_path: Path,output_file_path: Path):
    rid = get_dotnet_rid()
    source_path = source_path.resolve()

    package = ET.Element("package")

    metadata = ET.SubElement(package, "metadata")
    ET.SubElement(metadata, "id").text = f"{author}.{name}"
    ET.SubElement(metadata, "Title").text = name
    ET.SubElement(metadata, "version").text = version
    ET.SubElement(metadata, "authors").text = author
    ET.SubElement(metadata, "description").text = "Native binaries"

    package_types = ET.SubElement(metadata, "packageTypes")
    ET.SubElement(package_types, "packageType", {"name": "Dependency"})

    files_node = ET.SubElement(package, "files")
    
    for file_path in [x for pattern in ("*.dll", "*.so", "*.so.*", "*.dylib") for x in source_path.glob(pattern)]:
        if not file_path.is_file():
            continue

        file_path = file_path.resolve()

        ET.SubElement(
            files_node,
            "file",
            {
                "src": str(file_path),
                "target": f"runtimes/{rid}/native/{file_path.name}",
            },
        )

    ET.indent(package, space="  ")

    tree = ET.ElementTree(package)
    output_file_path.parent.mkdir(parents=True, exist_ok=True)
    tree.write(output_file_path, encoding="utf-8", xml_declaration=True)

    