from enum import Enum

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
    Literal = 0
    Struct = 1
    Enum = 2

class ParsedType:
    def __init__(self,type: EParsedType,tags: list[str]) -> None:
        self.name = ""
        self.type = type
        self.tags = tags
        self.body_line = ""
        self.namespace = ""

class ParsedStruct(ParsedType):
    def __init__(self,tags: list[str]) -> None:
        super().__init__(EParsedType.Struct,tags)
        self.fields: list[ParsedField] = []
        self.super = ""

class ParsedFile:
    def __init__(self) -> None:
        self.file_path = ""
        self.types: list[ParsedType] = []
        
