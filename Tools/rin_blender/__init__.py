import math

import bpy
from bpy.props import FloatProperty, StringProperty
from bpy_extras.io_utils import ExportHelper

# The glTF exporter's Standard lighting mode writes point and spot lights as W * 683 / 4pi candela and suns as
# W/m^2 * 683 lux, so these fields convert exactly to what Rin reads.
LUMENS_PER_WATT = 683.0

# Matches GltfSceneImporter: a point light reaches as far as it still gives this much light.
RADIUS_CUTOFF_LUX = 0.01

START_CAMERA_KEY = "rin_start_camera"


def watts_to_photometric(light):
    return light.energy * LUMENS_PER_WATT


def photometric_to_watts(light, value):
    light.energy = value / LUMENS_PER_WATT
    if light.type in {"POINT", "SPOT"}:
        match_rin_radius(light, value)


# EEVEE culls a light by its power alone, ignoring exposure, so physically dim lights vanish at high exposure.
def match_rin_radius(light, lumens):
    candela = lumens / (4.0 * math.pi)
    light.use_custom_distance = True
    light.cutoff_distance = math.sqrt(candela / RADIUS_CUTOFF_LUX)


class RIN_PT_light(bpy.types.Panel):
    bl_label = "Rin"
    bl_space_type = "PROPERTIES"
    bl_region_type = "WINDOW"
    bl_context = "data"

    @classmethod
    def poll(cls, context):
        return context.light is not None

    def draw(self, context):
        light = context.light
        if light.type == "SUN":
            self.layout.prop(light, "rin_lux")
        elif light.type in {"POINT", "SPOT"}:
            self.layout.prop(light, "rin_lumens")
            if light.type == "SPOT":
                self.layout.label(text="Rin imports spot lights as point lights", icon="INFO")
        else:
            self.layout.label(text="glTF does not export area lights", icon="ERROR")


class RIN_OT_export_level(bpy.types.Operator, ExportHelper):
    """Export the scene as a glTF level for Rin"""

    bl_idname = "export_scene.rin_level"
    bl_label = "Export Rin Level"
    filename_ext = ".glb"
    filter_glob: StringProperty(default="*.glb", options={"HIDDEN"})

    def execute(self, context):
        scene = context.scene
        if scene.camera is not None:
            scene[START_CAMERA_KEY] = scene.camera.name
        try:
            bpy.ops.export_scene.gltf(
                filepath=self.filepath,
                export_format="GLB",
                export_yup=True,
                export_lights=True,
                export_cameras=True,
                export_extras=True,
                export_import_convert_lighting_mode="SPEC",
            )
        finally:
            if START_CAMERA_KEY in scene:
                del scene[START_CAMERA_KEY]
        return {"FINISHED"}


def draw_export_menu(self, context):
    self.layout.operator(RIN_OT_export_level.bl_idname, text="Rin Level (.glb)")


classes = (RIN_PT_light, RIN_OT_export_level)


def register():
    for cls in classes:
        bpy.utils.register_class(cls)
    bpy.types.Light.rin_lumens = FloatProperty(
        name="Lumens", description="Point and spot light brightness in lumens. A household bulb is about 800",
        min=0.0, soft_max=100000.0, get=watts_to_photometric, set=photometric_to_watts)
    bpy.types.Light.rin_lux = FloatProperty(
        name="Lux", description="Sun brightness in lux. Direct sun is about 100,000",
        min=0.0, soft_max=150000.0, get=watts_to_photometric, set=photometric_to_watts)
    bpy.types.TOPBAR_MT_file_export.append(draw_export_menu)
    # Duplicates share their mesh, so a level stores and uploads each mesh once.
    bpy.context.preferences.edit.use_duplicate_mesh = False


def unregister():
    bpy.types.TOPBAR_MT_file_export.remove(draw_export_menu)
    del bpy.types.Light.rin_lux
    del bpy.types.Light.rin_lumens
    for cls in reversed(classes):
        bpy.utils.unregister_class(cls)
