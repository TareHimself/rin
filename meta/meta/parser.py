import re
import os
from io import TextIOWrapper
from typing import Union
from meta.constants import PROPERTY_REGEX, FUNCTION_REGEX, ARGUMENT_REGEX, STRUCT_NAME_REGEX
from meta.config import Config
from meta.types import (
    ParsedFile,
    ParsedStruct,
    ParsedFunction,
    ParsedProperty,
    ParsedFunctionArgument,
)


class FileParser:
    def __init__(self, config: Config, file_path: str) -> None:
        self.is_in_comment = False
        self.line_number_literal = 0
        self.line_number = 0
        self.lines: list[tuple[int, str]] = []
        self.file_path = file_path
        self.file: Union[None, TextIOWrapper] = None
        self.config = config
        self.depth = 0
        self.namespaces: list[tuple[str, int, int]] = []
        self.scope: list[tuple[str, str]] = []

    def get_namespace_for(self, line_no: int) -> str:
        within = list(filter(lambda a: a[1] <= line_no <= a[2], self.namespaces))
        within.sort(key=lambda a: a[1])
        return "::".join(map(lambda a: a[0], within))

    def reset(self):
        self.line_number = 0
        self.line_number_literal = 0

    def pre_process(self):

        with open(self.file_path, "r") as f:
            self.lines = [x.strip() for x in f.readlines()]

        namespaces: list[tuple[str, int, int]] = []

        line = self.read()

        while line is not None:
            if line.startswith("namespace") and '=' not in line:
                while "{" not in line:
                    line += self.read()

                temp = line[len("namespace ") : line.index("{")].strip()

                namespaces.append((temp, self.line_number, -1))
                self.depth += 1
            elif "{" in line or "}" in line:
                for char in line:
                    if char == "{":
                        self.depth += 1
                    elif char == "}":
                        self.depth -= 1
                        num_namespaces = len(namespaces)
                        if num_namespaces - self.depth > 0:
                            latest_namespace = namespaces.pop()

                            self.namespaces.append(
                                (
                                    latest_namespace[0],
                                    latest_namespace[1],
                                    self.line_number,
                                )
                            )

            line = self.read()

        self.reset()

    def _read_raw_line(self) -> Union[None, str]:
        if self.line_number_literal >= len(self.lines):
            return None

        if self.line_number_literal == 397:
            pass

        line: str = self.lines[self.line_number_literal]

        self.line_number_literal += 1

        if not (line.startswith("#if") or line.startswith("#endif")):
            self.line_number += 1

        return line

    def _read_no_comments(self, skip_comments=True) -> Union[None, str]:
        line = ""

        while True:
            line_temp = self._read_raw_line()

            if line_temp is None:
                return None

            line += line_temp.strip()

            if skip_comments:
                if self.is_in_comment:
                    if "*/" in line:
                        line = line[line.index("*/") + 1 :]
                        self.is_in_comment = False
                    else:
                        line = self._read_raw_line()

                elif "/*" in line:
                    line = line[line.index("/*") + 1 :]
                    self.is_in_comment = True
                elif "//" in line:
                    line = line[: line.index("//")].strip()

            if len(line) == 0 or self.is_in_comment:
                continue

            return line

    def read(self, skip_comments=True) -> Union[None, str]:
        return self._read_no_comments(skip_comments)

    def parse_tags(self, macro_line: str) -> list[str]:
        line = macro_line[
            macro_line.index("(")
            + 1 : len(macro_line[1:])
            - macro_line[::-1].index(")")
        ]
        return list(
            filter(
                lambda a: len(a) > 0, map(lambda a: a.strip(), line.strip().split(","))
            )
        )

    def parse_property(self, macro_iine: str) -> Union[None, ParsedProperty]:
        result = ParsedProperty(self.parse_tags(macro_iine))

        line = self.read()

        match_result = re.findall(PROPERTY_REGEX, line)

        if len(match_result) < 1 or len(match_result[0]) < 2:
            return None

        match_result = match_result[0]

        result.type = match_result[0]
        result.name = match_result[1]

        return result

    def parse_function(self, macro_iine: str) -> Union[None, ParsedFunction]:
        line = self.read()
        static_str = "static "

        result = ParsedFunction(self.parse_tags(macro_iine))

        match_result = re.findall(FUNCTION_REGEX, line)

        if len(match_result) < 1 or len(match_result[0]) < 3:
            return None

        match_result = match_result[0]

        result.is_static = not match_result[0].strip() == ""
        result.return_type = match_result[1]
        result.name = match_result[2]

        args_line = line.split("(")[1]

        while ")" not in args_line:
            args_line += self.read()

        args_line = args_line[: args_line.index(")")]

        args_arr = args_line.split(",")

        for arg in args_arr:
            match_result = re.findall(ARGUMENT_REGEX, arg)

            if len(match_result) < 1 or len(match_result[0]) < 2:
                continue

            match_result = match_result[0]

            p_arg = ParsedFunctionArgument()
            p_arg.name = match_result[1]
            p_arg.type = match_result[0]
            result.arguments.append(p_arg)

        return result

    def parse_struct(self, macro_line: str) -> Union[None, ParsedStruct]:
        tags = self.parse_tags(macro_line)
        search_tokens = ["struct ", "class ","enum "]
        line = self.read()
        token_found = ""
        while line is not None:
            for tok in search_tokens:
                if tok in line:
                    token_found = tok
                    break
            if token_found != "":
                break

            line = self.read()

        
        while line is not None and "{" not in line:
            temp = self.read()
            if temp is None:
                return None
            
            line += temp

        if line is None:
            return None
        
        re_result = list(filter(lambda a : a is not None,re.match(STRUCT_NAME_REGEX,line).groups()))

        struct_name = re_result[0]

        re_result = re_result[1:]

        if struct_name == "":
            pass

        super_struct = ""

        if len(re_result) > 1:
            super_struct = re_result[0].strip().split(" ")[-1]

        for tag in tags:
            arr = tag.split("=")
            if arr[0] == "Super":
                super_struct = arr[1]

        result = ParsedStruct(tags)
        result.name = struct_name
        result.super = super_struct
        result.namespace = self.get_namespace_for(self.line_number)

        line = "{".join(line.split('{')[1:])

        while line is not None:
            if line.startswith("};"):  # band aid
                return result

            if self.config.property_tag in line:
                r = self.parse_property(line)

                if r is not None:
                    result.fields.append(r)
            elif self.config.function_tag in line:
                r = self.parse_function(line)

                if r is not None:
                    result.fields.append(r)

            elif self.config.body_tag in line:
                result.body_line = f"{self.line_number}"

            line = self.read()

    def parse(self) -> Union[None, ParsedFile]:
        self.pre_process()

        result = ParsedFile()

        result.file_path = self.file_path

        line = self.read(False)

        if "// META_IGNORE" in line:
            return None

        while line is not None:
            if self.config.type_tag in line:
                r = self.parse_struct(line)
                if r is not None:
                    result.types.append(r)

            line = self.read()

        self.line_number = 0

        return result


class Parser:
    def __init__(self, config: Config, files: list[str]) -> None:
        self.files = files
        self.config = config

    def parse_file(self, file_path: str) -> Union[None, ParsedFile]:
        return FileParser(file_path=file_path, config=self.config).parse()

    def parse(self) -> list[ParsedFile]:
        results: list[ParsedFile] = []

        for file in self.files:
            print(f"Parsing {file}")

            result = self.parse_file(file)

            if result is not None and len(result.types) > 0:
                results.append(result)

            print(f"Parsed {file}")

        return results
