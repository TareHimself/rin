#!/usr/bin/env python3
"""
CLI tool for generating NuSpec files from source binaries.
"""

import argparse
import sys
from pathlib import Path

from dotnet import make_nuspec


def main():
    parser = argparse.ArgumentParser(
        description="Generate a NuSpec file for packaging native binaries",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
Examples:
  python make_nuspec_cli.py --name MyPackage --version 1.0.0 --author "Company" --source ./bin --output MyPackage.nuspec
  python make_nuspec_cli.py -n Rin.Graphics.Vulkan.Native -v 1.2.3 -a "Rin Team" -s ./native/build -o out.nuspec
        """,
    )

    parser.add_argument(
        "--name",
        "-n",
        required=True,
        help="Package name (e.g., 'Rin.Graphics.Vulkan.Native')",
    )
    parser.add_argument(
        "--version", "-v", required=True, help="Package version (e.g., '1.0.0')"
    )
    parser.add_argument(
        "--author", "-a", required=True, help="Package author (e.g., 'Rin Team')"
    )
    parser.add_argument(
        "--source",
        "-s",
        required=True,
        type=Path,
        help="Source directory containing binary files",
    )
    parser.add_argument(
        "--output", "-o", required=True, type=Path, help="Output directory"
    )

    args = parser.parse_args()

    try:
        # Validate source directory exists
        if not args.source.exists():
            print(
                f"Error: Source directory does not exist: {args.source}",
                file=sys.stderr,
            )
            return 1

        if not args.source.is_dir():
            print(
                f"Error: Source path is not a directory: {args.source}", file=sys.stderr
            )
            return 1

        # Generate the nuspec file
        make_nuspec(
            name=args.name,
            version=args.version,
            author=args.author,
            source_path=args.source,
            output_file_path=args.output,
        )

        print(f"NuSpec file created: {args.output.resolve()}")
        return 0

    except RuntimeError as e:
        print(f"Error: {e}", file=sys.stderr)
        return 1
    except Exception as e:
        print(f"Unexpected error: {e}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    sys.exit(main())
