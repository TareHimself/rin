# This file was created using https://github.com/TareHimself/python-extractor created by https://github.com/TareHimself

from enum import Enum
from io import TextIOWrapper
import re
from typing import Union
import os
import argparse



#########################################################
# FILE => D:\Github\reflect\python\reflect\constants.py #
#########################################################


REFLECT_CLASS_MACRO = "RCLASS("
REFLECT_STRUCT_MACRO = "RSTRUCT("
REFLECT_FUNCTION_MACRO = "RFUNCTION("
REFLECT_PROPERTY_MACRO = "RPROPERTY("
REFLECT_ARGUMENT_REGEX = "(?:const[\s]+?)?([a-zA-Z:0-9_*\s<>]+)(?:[&\s]+)([a-zA-Z0-9_]+)"
REFLECT_PROPERTY_REGEX = "([a-zA-Z0-9<> :*]+) ([a-zA-Z0-9_]+).*"
REFLECT_FUNCTION_REGEX = "(?:virtual\s)?(static|)([a-zA-Z0-9<>*_ :]+)\s([a-zA-Z0-9_]+)\("


#####################################################
# FILE => D:\Github\reflect\python\reflect\types.py #
#####################################################


class EParsedFieldType(Enum):
    Property = 0
    Function = 1
class ParsedField:
    def __init__(self,type: EParsedFieldType,tags: list[str]) -> None:
        self.field_type = type
        self.tags = tags
class ParsedFunctionArgument:
    def __init__(self) -> None:
        self.type = ""
        self.name = ""
class ParsedFunction(ParsedField):
    def __init__(self,tags: list[str]) -> None:
        super().__init__(EParsedFieldType.Function,tags)
        self.name = ""
        self.return_type = ""
        self.is_static = False
        self.arguments: list[ParsedFunctionArgument] = []
class ParsedProperty(ParsedField):
    def __init__(self,tags: list[str]) -> None:
        super().__init__(EParsedFieldType.Property,tags)
        self.name = ""
        self.type = ""
class EParsedType(Enum):
    Class = 0
    Struct = 1
    Enum = 2
class ParsedType:
    def __init__(self,type: EParsedType,tags: list[str]) -> None:
        self.name = ""
        self.type = type
        self.tags = tags
class ParsedStruct(ParsedType):
    def __init__(self,tags: list[str]) -> None:
        super().__init__(EParsedType.Struct,tags)
        self.fields: list[ParsedField] = []
class ParsedClass(ParsedStruct):
    def __init__(self,tags: list[str]) -> None:
        super().__init__(tags)
        self.type = EParsedType.Class
class ParsedFile:
    def __init__(self) -> None:
        self.file_path = ""
        self.types: list[ParsedType] = []


########################################################
# FILE => D:\Github\reflect\python\reflect\generate.py #
########################################################


def write_line(file: TextIOWrapper,line: str) -> None:
    file.write(f"{line}\n")
class IncludeGuard():
    def __init__(self,file: TextIOWrapper,name: str):
        self.file = file
        self.name = name
         
    def __enter__(self):
        write_line(self.file,f"#ifndef _REFLECT_GENERATED_{self.name}")
        write_line(self.file,f"#define _REFLECT_GENERATED_{self.name}")
        return self
     
    def __exit__(self, exc_type, exc_value, exc_traceback):
        write_line(self.file,"#endif")
def include(file: TextIOWrapper,header: str) -> None:
    write_line(file,f"#include \"{header}\"")
def write_property(file: TextIOWrapper,type_name: str,field: ParsedProperty) -> tuple[str,list[str]]:
    macro = f"_REFLECTED_GENERATED_{type_name}_PROPERTY_{field.name}"

    write_line(file,f"#define {macro} REFLECT_WRAP_PROPERTY({type_name},{field.name},{field.type})")

    write_line(file,"")

    return [macro,field.tags]
def write_function(file: TextIOWrapper,type_name: str,field: ParsedFunction) -> tuple[str,list[str]]:
    macro = f"_REFLECTED_GENERATED_{type_name}_FUNCTION_{field.name}"

    write_line(file,f"#define {macro} REFLECT_WRAP_FUNCTION_BEGIN({field.name}) \\")
    
    write_line(file,"{ \\")

    arg_names: list[str] = []

    i = 0
    for arg in field.arguments:
        arg_name = f"arg_{i}"

        write_line(file,f"auto {arg_name} = args[{i}].As<{arg.type}>(); \\")

        arg_names.append(arg_name)

        i += 1

    if field.is_static:
        func_call = f"{type_name}::{field.name}("
    else:
        func_call = f"instance.As<{type_name}>()->{field.name}("

    for arg_name in arg_names:
        func_call += f"*{arg_name}"

        if arg_name != arg_names[-1]:
            func_call += ","
    
    func_call += ");"

    if field.return_type != "void":
        checked_call = "if(result){ \\\n"
        checked_call += f"*result.As<{field.return_type}>() = {func_call} \\\n"
        checked_call += "}"
        func_call = checked_call

    write_line(file," \\")
    write_line(file,f"{func_call} \\")
    write_line(file,"})\n")

    return [macro,field.tags]
def write_builder(file: TextIOWrapper,type: ParsedType,macros:  list[tuple[str,list[str]]]) -> None:
    write_line(file,f"#define _REFLECT_GENERATE_{type.name} \\")
    write_line(file,"reflect::factory::TypeBuilder builder; \\")

    for macro,tags in macros:
        write_line(file,"{ \\")
        write_line(file,f"  auto field = {macro}; \\")
        for tag in  tags:
            write_line(file,f"  field->AddTag(\"{tag}\"); \\") 
        write_line(file,f"  builder.AddField(field); \\")
        write_line(file,"} \\")

    write_line(file,f"auto created = builder.Create<{type.name}>(\"{type.name}\"); \\")

    for tag in  type.tags:
        write_line(file,f"created->AddTag(\"{tag}\"); \\") 

    write_line(file,"")
def write_fields(file: TextIOWrapper,type_name: str,fields: list[ParsedField]) -> list[str]:
    macros: list[str] = []

    for field in fields:
        if field.field_type == EParsedFieldType.Function:
            macros.append(write_function(file,type_name,field))
        elif field.field_type == EParsedFieldType.Property:
            macros.append(write_property(file,type_name,field))

    return macros
def write_class(file: TextIOWrapper,item: ParsedClass) -> None:
    with IncludeGuard(file,item.name):

        macros = write_fields(file,item.name,item.fields)

        write_builder(file,item,macros)
def write_struct(file: TextIOWrapper,item: ParsedStruct) -> None:
    with IncludeGuard(file,item.name):
        
        macros = write_fields(file,item.name,item.fields)

        write_builder(file,item,macros)
def generate(parsed_file: ParsedFile,file_path: str):
    with open(file_path,'w') as f:
        write_line(f,"#pragma once")
        include(f,"reflect/Macro.hpp")
        include(f,"reflect/Reflect.hpp")
        include(f,"reflect/Factory.hpp")
        include(f,"reflect/wrap/Wrap.hpp")
        write_line(f,"")
        write_line(f,"")

        for parsed in parsed_file.types:

            if parsed.type == EParsedType.Class:
                write_class(f,parsed)
            elif parsed.type == EParsedType.Struct:
                write_struct(f,parsed)

            write_line(f,"")
            write_line(f,"")


######################################################
# FILE => D:\Github\reflect\python\reflect\parser.py #
######################################################


class FileParser:
    def __init__(self,file_path: str) -> None:
        self.is_in_comment = False
        self.line_number = 0
        self.file_path = file_path
        self.file: Union[None,TextIOWrapper] = None

    def _read(self) -> Union[None,str]:
        if self.file is None:
            return None
        
        line = self.file.readline()

        if line == "":
            return None

        self.line_number += 1

        return line

    def read(self) -> Union[None,str]:
        
        line = ""

        while True:
            line_temp = self._read()

            if line_temp is None:
                return None
            
            line += line_temp.strip()

            if self.is_in_comment:
                if "*/" in line:
                    line = line[line.index("*/") + 1:]
                    self.is_in_comment = False
                else:
                    line = self._read()

            elif "/*" in line:
                line = line[line.index("/*") + 1:]
                self.is_in_comment = True
            elif "//" in line:
                line = line[:line.index("//")]

            if len(line) == 0 or self.is_in_comment:
                continue

            return line
            


    def parse_tags(self,macro_line:str) -> list[str]:
        line =  macro_line[macro_line.index('(') + 1:len(macro_line[1:]) - macro_line[::-1].index(')')]
        return  list(filter(lambda a : len(a) > 0,map(lambda a : a.strip(),line.strip().split(','))))

    def parse_property(self,macro_iine:str) -> Union[None,ParsedProperty]:
        result  = ParsedProperty(self.parse_tags(macro_iine))

        line = self.read()

        match_result = re.findall(REFLECT_PROPERTY_REGEX,line)

        if len(match_result) < 1 or len(match_result[0]) < 2:
            return None
        
        match_result = match_result[0]
        
        result.type = match_result[0]
        result.name = match_result[1]

        return result



    def parse_function(self,macro_iine:str) -> Union[None,ParsedFunction]:
        line = self.read()
        static_str = "static "

        result = ParsedFunction(self.parse_tags(macro_iine))

        match_result = re.findall(REFLECT_FUNCTION_REGEX,line)

        if len(match_result) < 1 or len(match_result[0]) < 3:
            return None
        
        match_result = match_result[0]
        
        result.is_static = not match_result[0].strip() == ""
        result.return_type = match_result[1]
        result.name = match_result[2]

        args_line = line.split("(")[1]

        while ")" not in args_line:
            args_line += self.read()

        args_line = args_line[:args_line.index(")")]

        args_arr = args_line.split(",")

        for arg in args_arr:
            match_result = re.findall(REFLECT_ARGUMENT_REGEX,arg)

            if len(match_result) < 1 or len(match_result[0]) < 2:
                continue

            match_result = match_result[0]

            p_arg = ParsedFunctionArgument()
            p_arg.name = match_result[1]
            p_arg.type = match_result[0]
            result.arguments.append(p_arg)

        return result

    def parse_class(self,macro_iine:str) -> Union[None,ParsedClass]:
        search_str = "class "
        line = self.read()

        while line is not None and search_str not in line:
            line = self.read()

        if line is None:
            return None
        
        split_result = line[line.index(search_str) + len(search_str):].split(" ")

        if len(split_result) == 0:
            return None
        
        result = ParsedClass(self.parse_tags(macro_iine))
        result.name = split_result[0]

        line = self.read()
        
        while line is not None:
            if "};" in line:
                return result
            
            if REFLECT_PROPERTY_MACRO in line:
                r = self.parse_property(line)

                if r is not None:
                    result.fields.append(r)
            elif REFLECT_FUNCTION_MACRO in line:
                r = self.parse_function(line)

                if r is not None:
                    result.fields.append(r)

            line = self.read()





    def parse_struct(self,macro_iine:str) -> Union[None,ParsedStruct]:
        search_str = "struct "
        line = self.read()

        while line is not None and search_str not in line:
            line = self.read()

        if line is None:
            return None
        
        split_result = line[line.index(search_str) + len(search_str):].split(" ")

        if len(split_result) == 0:
            return None
        
        result = ParsedStruct(self.parse_tags(macro_iine))
        result.name = split_result[0]

        line = self.read()
        
        while line is not None:
            if "};" in line:
                return result
            
            if REFLECT_PROPERTY_MACRO in line:
                r = self.parse_property(line)

                if r is not None:
                    result.fields.append(r)
            elif REFLECT_FUNCTION_MACRO in line:
                r = self.parse_function(line)

                if r is not None:
                    result.fields.append(r)

            line = self.read()

    def parse(self) -> Union[None,ParsedFile]:
        with open(self.file_path,'r') as f:
            self.file = f

            result = ParsedFile()

            result.file_path = self.file_path

            line = self.read()

            while line is not None:
                if REFLECT_CLASS_MACRO in line:
                    r = self.parse_class(line)
                    if r is not None:
                        result.types.append(r)
                elif REFLECT_STRUCT_MACRO in line:
                    r = self.parse_struct(line)
                    if r is not None:
                        result.types.append(r)
                
                line = self.read()

            self.line_number = 0

            return result
class Parser:
    def __init__(self,files: list[str]) -> None:
        self.files = files

    def parse_file(self,file_path: str) -> Union[None,ParsedFile]:
        parser = FileParser(file_path=file_path)

        return parser.parse()

    def parse(self) -> list[ParsedFile]:
        results: list[ParsedFile] = []

        for file in self.files:
            print(f"Parsing {file}")

            result = self.parse_file(file)

            if result is not None and len(result.types) > 0:
                results.append(result)

            print(f"Parsed {file}")

        return results


############################################
# FILE => D:\Github\reflect\python\main.py #
############################################


class SmartFormatter(argparse.HelpFormatter):
    def _split_lines(self, text, width):
        if text.startswith("R|"):
            return text[2:].splitlines()
        # this is the RawTextHelpFormatter._split_lines
        return argparse.HelpFormatter._split_lines(self, text, width)
def search_for_headers(dir: str,header_ext: str) -> list[str]:
    results: list[str] = []
    
    for entry in os.listdir(dir):
        entry = os.path.join(dir,entry)
        if os.path.isdir(entry):
            results = results + search_for_headers(entry,header_ext)
        elif entry.endswith(f".{header_ext}") and not entry.endswith(f"reflect.{header_ext}"):
            results.append(entry)

    return results
def main():
    parser = argparse.ArgumentParser(
        prog="Reflect Header Generator",
        description="Generates headers that can be used with Reflect",
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
        "-f",
        "--filter",
        type=str,
        default="hpp",
        help="Filter extension for source files.",
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
    filter_ext = args.filter

    files_to_parse = search_for_headers(source_dir,filter_ext)

    p = Parser(files_to_parse)

    parsed_files = p.parse()

    for parsed_file in parsed_files:
        new_relative_path = parsed_file.file_path[len(source_dir) + 1:-(len(filter_ext))] + f"reflect.{filter_ext}"
        out_file_path = os.path.join(output_dir,new_relative_path)
        dir_name = os.path.dirname(out_file_path)
        if not os.path.exists(dir_name):
            os.makedirs(dir_name)
        generate(parsed_file,out_file_path)



if __name__ == "__main__":
    main()