from meta.parser import Parser, FileParser
from meta.generate import generate
from meta.config import Config
from meta.watcher import FileWatcher
import os
import argparse

class SmartFormatter(argparse.HelpFormatter):
    def _split_lines(self, text, width):
        if text.startswith("R|"):
            return text[2:].splitlines()
        # this is the RawTextHelpFormatter._split_lines
        return argparse.HelpFormatter._split_lines(self, text, width)
    
def search_for_headers(dir: str,config: Config) -> list[str]:
    results: list[str] = []
    
    for entry in os.listdir(dir):
        entry = os.path.join(dir,entry)
        if os.path.isdir(entry):
            results = results + search_for_headers(entry,config)
        elif entry.endswith(f".{config.header_extension}") and not entry.endswith(f"meta.{config.header_extension}"):
            results.append(entry)

    return results
            
def main():
    parser = argparse.ArgumentParser(
        prog="Meta Header Generator",
        description="Generates headers that can be used with Meta",
        formatter_class=SmartFormatter,
        exit_on_error=True
    )

    parser.add_argument(
        "-s",
        "--source",
        type=str,
        help="The directory to search for source files.",
        required=True
    )

    parser.add_argument(
        "-c",
        "--config",
        type=str,
        default=os.path.join(os.getcwd(),"meta","config.json"),
        help="Config for generation .",
        required=False
    )

    parser.add_argument(
        "-w",
        "--watch",
        action=argparse.BooleanOptionalAction,
        help="Watch for file changes ?",
        required=False
    )

    parser.add_argument(
        "-o",
        "--output",
        type=str,
        help="The directory to output to. Defaults to the source directory.",
        required=False
    )

    args = parser.parse_args()

    source_dir = os.path.abspath(args.source)
    output_dir = os.path.abspath(args.output if args.output is not None else source_dir)
    config = Config(os.path.abspath(args.config))

    files_to_parse = search_for_headers(source_dir,config)

    p = Parser(config,files_to_parse)

    parsed_files = p.parse()

    

        

    for parsed_file in parsed_files:
        new_relative_path = parsed_file.file_path[len(source_dir) + 1:-(len(config.header_extension))] + f"{config.meta_extension}."
        out_file_path = os.path.join(output_dir,new_relative_path)
        dir_name = os.path.dirname(out_file_path)
        if not os.path.exists(dir_name):
            os.makedirs(dir_name)
        generate(parsed_file,out_file_path,config)

    if args.watch:
        def generate_file(file_path: str):
            nonlocal config
            nonlocal source_dir
            nonlocal output_dir
            print(f"Re-Parsing {file_path}")
            re_parsed = FileParser(file_path=file_path,config=config).parse()
            print(f"Parsed {file_path}")
            if re_parsed is not None:
                new_relative_path = re_parsed.file_path[len(source_dir) + 1:-(len(config.header_extension))] + f"{config.meta_extension}."
                out_file_path = os.path.join(output_dir,new_relative_path)
                dir_name = os.path.dirname(out_file_path)
                if not os.path.exists(dir_name):
                    os.makedirs(dir_name)
                generate(re_parsed,out_file_path,config)

        FileWatcher([x.file_path for x in parsed_files],generate_file).watch()
    


if __name__ == "__main__":
    main()