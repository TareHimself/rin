import json
import re

import bpy
from bpy_extras.io_utils import ImportHelper
from bpy.props import (BoolProperty, CollectionProperty, EnumProperty, FloatProperty, FloatVectorProperty,
                       IntProperty, PointerProperty, StringProperty)

COMPONENTS_KEY = "rin_components"

# The glTF exporter skips properties whose names start with "gltf", so only the rin_components extra written at
# export time reaches the file.
OBJECT_PROPERTY = "gltf_rin"

PREVIEW_OF_KEY = "rin_preview_of"

# Called with the object whenever one of its components is added, removed or edited.
change_listeners = []

# Each takes (object, warnings, level folder) and returns (type name, fields) or None, replacing the generic export of
# that component.
export_sources = []

# Components whose panel another module draws, so no generic panel is made for them.
custom_panels = set()

# Components Blender already authors as an object's transform, camera and parent, so they get no panel.
NATIVE_COMPONENTS = {
    "Rin.World.Transforms.LocalTransform",
    "Rin.World.Transforms.AttachSocket",
    "Rin.World.Graphics.Camera",
}

COLOR_TYPE = "Rin.Core.Views.Color"

# Each draws into the components panel first, and returns True when the object takes no components.
panel_drawers = []


_types = {}
_groups = []
_panels = []


def label_of(name):
    return re.sub(r"(?<=[a-z0-9])(?=[A-Z])", " ", name)


def group_attribute(type_name):
    return "c_" + re.sub(r"\W", "_", type_name)


def components_of(obj):
    return getattr(obj, OBJECT_PROPERTY)


def is_preview(obj):
    return PREVIEW_OF_KEY in obj


def is_loaded(type_name):
    return type_name in _types


def type_entry_of(type_name):
    return _types[type_name]


def notify_changed(obj):
    for listener in change_listeners:
        listener(obj)


def on_field_changed(fields, _context):
    notify_changed(fields.id_data)


class RIN_PG_component_entry(bpy.types.PropertyGroup):
    pass


class RIN_PG_components(bpy.types.PropertyGroup):
    added: CollectionProperty(type=RIN_PG_component_entry)

    def has(self, type_name):
        return type_name in self.added

    def fields_of(self, type_name):
        return getattr(self, group_attribute(type_name))


# The schema describes a color as a struct of four floats, which Blender edits as a color.
def kind_of(field):
    if field["type"] == "struct" and field.get("typeName") == COLOR_TYPE:
        return "color"
    return field["type"]


def field_property(field, default):
    common = {"name": label_of(field["name"]), "update": on_field_changed}
    kind = kind_of(field)
    if kind == "float":
        return FloatProperty(default=default if default is not None else 0.0, **common)
    if kind == "int":
        return IntProperty(default=default if default is not None else 0, **common)
    if kind == "bool":
        return BoolProperty(default=bool(default), **common)
    if kind == "vector3":
        return FloatVectorProperty(size=3, default=default or (0.0, 0.0, 0.0),
                                   description="In Rin's axes: +X right, +Y up, +Z forward", **common)
    if kind == "quaternion":
        return FloatVectorProperty(size=4, default=default or (0.0, 0.0, 0.0, 1.0),
                                   description="X, Y, Z, W in Rin's axes", **common)
    if kind == "color":
        return FloatVectorProperty(size=4, subtype="COLOR", min=0.0, soft_max=1.0,
                                   default=default or (1.0, 1.0, 1.0, 1.0), **common)
    if kind == "enum":
        items = [(value, label_of(value), "") for value in field["enumNames"]]
        return EnumProperty(items=items, default=default if default in field["enumNames"] else items[0][0], **common)
    if kind == "entity":
        return PointerProperty(type=bpy.types.Object, description="Exported as the object's name", **common)
    return None


def export_value(fields, field):
    value = getattr(fields, field["name"])
    kind = kind_of(field)
    if kind == "entity":
        return value.name if value is not None else None
    if kind in {"vector3", "quaternion", "color"}:
        return [float(v) for v in value]
    return value


def editable_fields(type_entry):
    return [field for field in type_entry["fields"] if field_property(field, None) is not None]


def field_value(obj, type_name, field_name):
    return getattr(components_of(obj).fields_of(type_name), field_name)


def load_schema(path):
    with open(bpy.path.abspath(path), encoding="utf-8") as file:
        schema = json.load(file)
    unload_schema()
    type_entries = [entry for entry in schema["components"] if entry["name"] not in NATIVE_COMPONENTS]
    for type_entry in type_entries:
        register_group(type_entry)
    for type_entry in sorted(type_entries, key=display_name_of):
        if type_entry["name"] not in custom_panels:
            register_panels(type_entry)
    for obj in bpy.data.objects:
        notify_changed(obj)
    return len(_types)


def register_group(type_entry):
    defaults = type_entry.get("defaults", {})
    annotations = {}
    for field in editable_fields(type_entry):
        annotations[field["name"]] = field_property(field, defaults.get(field["name"]))
    group = type(group_attribute(type_entry["name"]), (bpy.types.PropertyGroup,), {"__annotations__": annotations})
    bpy.utils.register_class(group)
    setattr(RIN_PG_components, group_attribute(type_entry["name"]), PointerProperty(type=group))
    _types[type_entry["name"]] = type_entry
    _groups.append(group)


def display_name_of(type_entry):
    return label_of(type_entry["displayName"])


# A top-level panel per component type and per place (Properties editor, 3D Viewport sidebar), so each can be
# collapsed and dragged on its own, shown only on objects that have the component. Panels are global classes, so
# their order, registration order or wherever they were dragged, is the same on every object.
def register_panels(type_entry):
    for parent in (RIN_PT_components, RIN_PT_components_sidebar):
        panel = type(f"{parent.__name__}_{group_attribute(type_entry['name'])}", (ComponentFieldsPanel, bpy.types.Panel), {
            "bl_label": display_name_of(type_entry),
            "bl_space_type": parent.bl_space_type,
            "bl_region_type": parent.bl_region_type,
            "type_name": type_entry["name"],
        })
        if hasattr(parent, "bl_context"):
            panel.bl_context = parent.bl_context
        if hasattr(parent, "bl_category"):
            panel.bl_category = parent.bl_category
        bpy.utils.register_class(panel)
        _panels.append(panel)


def unload_schema():
    for panel in reversed(_panels):
        bpy.utils.unregister_class(panel)
    _panels.clear()
    for type_name in _types:
        delattr(RIN_PG_components, group_attribute(type_name))
    for group in reversed(_groups):
        bpy.utils.unregister_class(group)
    _types.clear()
    _groups.clear()


def write_extras(scene, warnings, folder):
    written = []
    for obj in scene.objects:
        components = {}
        for entry in components_of(obj).added:
            if entry.name in _types:
                components[entry.name] = export_fields(obj, _types[entry.name])
        for source in export_sources:
            if (derived := source(obj, warnings, folder)) is not None:
                components[derived[0]] = derived[1]
        if components:
            obj[COMPONENTS_KEY] = components
            written.append(obj)
    return written


def export_fields(obj, type_entry):
    fields = components_of(obj).fields_of(type_entry["name"])
    values = {}
    for field in editable_fields(type_entry):
        value = export_value(fields, field)
        if value is not None:
            values[field["name"]] = value
    return values


def clear_extras(objects):
    for obj in objects:
        del obj[COMPONENTS_KEY]


def add_component(obj, type_name):
    components = components_of(obj)
    if type_name not in _types or components.has(type_name):
        return False
    components.added.add().name = type_name
    type_entry = _types[type_name]
    fields = components.fields_of(type_name)
    for field in editable_fields(type_entry):
        fields.property_unset(field["name"])
    notify_changed(obj)
    return True


def remove_component(obj, type_name):
    added = components_of(obj).added
    index = added.find(type_name)
    if index < 0:
        return False
    added.remove(index)
    notify_changed(obj)
    return True


class RIN_OT_add_component(bpy.types.Operator):
    """Add a Rin component to the active object"""

    bl_idname = "object.rin_add_component"
    bl_label = "Add Rin Component"
    bl_options = {"REGISTER", "UNDO"}
    type_name: StringProperty()

    def execute(self, context):
        return {"FINISHED"} if add_component(context.object, self.type_name) else {"CANCELLED"}


class RIN_OT_remove_component(bpy.types.Operator):
    """Remove this Rin component from the active object"""

    bl_idname = "object.rin_remove_component"
    bl_label = "Remove Rin Component"
    bl_options = {"REGISTER", "UNDO"}
    type_name: StringProperty()

    def invoke(self, context, event):
        name = display_name_of(_types[self.type_name]) if self.type_name in _types else self.type_name
        return context.window_manager.invoke_confirm(
            self, event, title=f"Remove {name}?", message="Its fields are reset if you add it again.",
            confirm_text="Remove", icon="WARNING")

    def execute(self, context):
        return {"FINISHED"} if remove_component(context.object, self.type_name) else {"CANCELLED"}


class RIN_OT_reload_schema(bpy.types.Operator):
    """Reload the component schema from the path in the add-on preferences"""

    bl_idname = "preferences.rin_reload_schema"
    bl_label = "Reload Schema"

    def execute(self, context):
        try:
            count = load_schema(preferences(context).schema_path)
        except (OSError, ValueError, KeyError) as error:
            self.report({"ERROR"}, f"Could not read the Rin schema: {error}")
            return {"CANCELLED"}
        self.report({"INFO"}, f"Loaded {count} Rin components")
        return {"FINISHED"}


class RIN_OT_pick_schema(bpy.types.Operator, ImportHelper):
    """Pick the component schema rin-editor-schema wrote"""

    bl_idname = "preferences.rin_pick_schema"
    bl_label = "Pick Schema"
    filename_ext = ".schema.json"
    filter_glob: StringProperty(default="*.schema.json", options={"HIDDEN"})

    def execute(self, context):
        preferences(context).schema_path = self.filepath
        return {"FINISHED"}


def reload_from_preferences(_preferences=None, _context=None):
    if preferences(bpy.context) is not None and preferences(bpy.context).schema_path:
        bpy.ops.preferences.rin_reload_schema()


def preferences(context):
    addon = context.preferences.addons.get(__package__)
    return addon.preferences if addon is not None else None


class RIN_AP_preferences(bpy.types.AddonPreferences):
    bl_idname = __package__

    schema_path: StringProperty(
        name="Component Schema", update=reload_from_preferences, description="The file rin-editor-schema writes")

    def draw(self, context):
        row = self.layout.row()
        row.prop(self, "schema_path")
        row.operator(RIN_OT_pick_schema.bl_idname, text="", icon="FILEBROWSER")
        row.operator(RIN_OT_reload_schema.bl_idname, text="", icon="FILE_REFRESH")


class RIN_MT_add_component(bpy.types.Menu):
    bl_label = "Add Component"

    def draw(self, context):
        components = components_of(context.object)
        available = [entry for name, entry in _types.items() if not components.has(name)]
        if not available:
            self.layout.label(text="Every component is added")
        for entry in sorted(available, key=display_name_of):
            self.layout.operator(RIN_OT_add_component.bl_idname, text=display_name_of(entry)).type_name = entry["name"]


class ComponentsPanel:
    bl_label = "Rin Components"

    @classmethod
    def poll(cls, context):
        return context.object is not None and not is_preview(context.object)

    def draw(self, context):
        layout = self.layout
        for drawer in panel_drawers:
            if drawer(layout, context.object):
                return
        if not _types:
            layout.label(text="Set the schema path in the Rin add-on preferences", icon="INFO")
            return
        layout.menu(RIN_MT_add_component.__name__, icon="ADD")
        for entry in components_of(context.object).added:
            if entry.name not in _types:
                draw_unknown_component(layout.box(), entry.name)


def draw_unknown_component(box, type_name):
    header = box.row()
    header.label(text=type_name, icon="ERROR")
    header.operator(RIN_OT_remove_component.bl_idname, text="", icon="X", emboss=False).type_name = type_name
    box.label(text="Not in the schema, so it is not exported")


class ComponentFieldsPanel:
    type_name = ""

    @classmethod
    def poll(cls, context):
        obj = context.object
        return obj is not None and components_of(obj).has(cls.type_name)

    def draw_header_preset(self, context):
        self.layout.operator(RIN_OT_remove_component.bl_idname, text="", icon="X", emboss=False).type_name = self.type_name

    def draw(self, context):
        draw_fields(self.layout, context.object, self.type_name)


def draw_fields(layout, obj, type_name, skip=()):
    type_entry = _types[type_name]
    fields = components_of(obj).fields_of(type_name)
    for field in editable_fields(type_entry):
        if field["name"] in skip:
            continue
        layout.prop(fields, field["name"])


class RIN_PT_components(ComponentsPanel, bpy.types.Panel):
    bl_space_type = "PROPERTIES"
    bl_region_type = "WINDOW"
    bl_context = "object"


class RIN_PT_components_sidebar(ComponentsPanel, bpy.types.Panel):
    bl_space_type = "VIEW_3D"
    bl_region_type = "UI"
    bl_category = "Rin"


classes = (RIN_PG_component_entry, RIN_PG_components, RIN_OT_add_component, RIN_OT_remove_component,
           RIN_OT_reload_schema, RIN_OT_pick_schema, RIN_AP_preferences, RIN_MT_add_component, RIN_PT_components,
           RIN_PT_components_sidebar)


def register():
    for cls in classes:
        bpy.utils.register_class(cls)
    setattr(bpy.types.Object, OBJECT_PROPERTY, PointerProperty(type=RIN_PG_components))
    # Preferences are readable only once registration finishes.
    bpy.app.timers.register(reload_from_preferences, first_interval=0.0)


def unregister():
    unload_schema()
    delattr(bpy.types.Object, OBJECT_PROPERTY)
    for cls in reversed(classes):
        bpy.utils.unregister_class(cls)
