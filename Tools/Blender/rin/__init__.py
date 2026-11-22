import os

import bpy
from bpy.props import BoolProperty, StringProperty

from . import colliders, components, environment, physics, previews, visibility
from .level_folder import LevelFolder

START_CAMERA_KEY = "rin_start_camera"


class RIN_PT_light(bpy.types.Panel):
    bl_label = "Rin"
    bl_space_type = "PROPERTIES"
    bl_region_type = "WINDOW"
    bl_context = "data"

    @classmethod
    def poll(cls, context):
        return context.light is not None

    def draw(self, context):
        obj = context.object
        if obj is not None and components.is_preview(obj) and obj.parent is not None:
            self.layout.label(text=f"Preview of the light component on {obj.parent.name}", icon="INFO")
        else:
            self.layout.label(text="Rin does not export Blender lights", icon="ERROR")
            self.layout.label(text="Add a Point Light or Directional Light component to an empty instead")


class RIN_OT_export_level(bpy.types.Operator):
    """Export the scene as a Rin level folder: level.glb plus the files it uses"""

    bl_idname = "export_scene.rin_level"
    bl_label = "Export Rin Level"
    directory: StringProperty(subtype="DIR_PATH")
    filter_folder: BoolProperty(default=True, options={"HIDDEN"})

    def invoke(self, context, _event):
        context.window_manager.fileselect_add(self)
        return {"RUNNING_MODAL"}

    def execute(self, context):
        if not self.directory:
            self.report({"ERROR"}, "Pick a folder for the level")
            return {"CANCELLED"}
        folder = LevelFolder(bpy.path.abspath(self.directory))
        os.makedirs(folder.directory, exist_ok=True)
        scene = context.scene
        if scene.camera is not None:
            scene[START_CAMERA_KEY] = scene.camera.name
        warnings = []
        with_components = components.write_extras(scene, warnings, folder)
        with_colliders = colliders.write_extras(scene, warnings)
        try:
            with previews.ExportExclusion(context):
                bpy.ops.export_scene.gltf(
                    filepath=folder.level_path,
                    export_format="GLB",
                    export_yup=True,
                    export_lights=False,
                    export_cameras=True,
                    export_extras=True,
                )
        finally:
            components.clear_extras(with_components)
            colliders.clear_extras(with_colliders)
            if START_CAMERA_KEY in scene:
                del scene[START_CAMERA_KEY]
        lights = [obj.name for obj in scene.objects if obj.type == "LIGHT" and not components.is_preview(obj)]
        if lights:
            warnings.append(f"Blender lights are not exported: {', '.join(lights)}")
        for warning in warnings:
            self.report({"WARNING"}, warning)
        self.report({"INFO"}, f"Exported the level to {folder.directory}")
        return {"FINISHED"}


def draw_export_menu(self, context):
    self.layout.operator(RIN_OT_export_level.bl_idname, text="Rin Level (folder)")


classes = (RIN_PT_light, RIN_OT_export_level)


def register():
    for cls in classes:
        bpy.utils.register_class(cls)
    components.register()
    previews.register()
    physics.register()
    colliders.register()
    environment.register()
    visibility.register()
    bpy.types.TOPBAR_MT_file_export.append(draw_export_menu)
    # Duplicates share their mesh, so a level stores and uploads each mesh once.
    bpy.context.preferences.edit.use_duplicate_mesh = False


def unregister():
    bpy.types.TOPBAR_MT_file_export.remove(draw_export_menu)
    visibility.unregister()
    environment.unregister()
    colliders.unregister()
    physics.unregister()
    previews.unregister()
    components.unregister()
    for cls in reversed(classes):
        bpy.utils.unregister_class(cls)
