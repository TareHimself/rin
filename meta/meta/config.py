import json

class Config:
    def __init__(self,config_path: str) -> None:
        self.includes: list[str] = []
        self.type_tag = ""
        self.property_tag = ""
        self.function_tag = ""
        self.body_tag = ""
        self.constructor_tag = ""
        self.function_create = ""
        self.property_create = ""
        self.property_create = ""
        self.property_create = ""
        self.header_extension = ""
        self.source_extension = ""
        self.namespace = ""
        self.smart_pointer = ""
        self.smart_pointer_allocator = ""
        self.vector = ""
        self.meta_extension = ""

        with open(config_path, 'r') as f:
            data = json.load(f)
            self.includes = data["includes"]
            self.type_tag = data["type_tag"]
            self.property_tag = data["property_tag"]
            self.function_tag = data["function_tag"]
            self.body_tag = data["body_tag"]
            self.constructor_tag = data["constructor_tag"]
            self.function_create = data["function_create"]
            self.property_create = data["property_create"]
            self.header_extension = data["header_extension"]
            self.source_extension = data["source_extension"]
            self.namespace = data["namespace"]
            self.smart_pointer = data["smart_pointer"]
            self.smart_pointer_allocator = data["smart_pointer_allocator"]
            self.vector = data["vector"]
            self.meta_extension = data["meta_extension"]
