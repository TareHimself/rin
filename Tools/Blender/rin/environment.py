import math

import bpy
from bpy.app.handlers import persistent
from bpy.props import BoolProperty, PointerProperty, StringProperty
from bpy_extras.io_utils import ImportHelper

from . import components, previews

ENVIRONMENT_LIGHT = "Rin.World.Graphics.ImageBasedLighting.EnvironmentLight"
MAP_FOLDER = "environments"

# The glTF exporter skips properties whose names start with "gltf".
MAP_PROPERTY = "gltf_rin_environment_map"
UNBOUNDED_PROPERTY = "gltf_rin_environment_unbounded"
PREVIOUS_WORLD_PROPERTY = "gltf_rin_previous_world"

WORLD_NAME = "Rin Preview World"
WORLD_KEY = "rin_preview_world"

# Rin puts a panorama's centre on its forward axis, Blender's -Y; Blender's Environment Texture puts it on +X.
PANORAMA_TURN = (0.0, 0.0, math.pi / 2)


def has_environment(obj):
    return components.components_of(obj).has(ENVIRONMENT_LIGHT)


# The box is the empty's Cube display, unscaled: Rin applies the entity's scale on top. Blender's X, Y, Z are Rin's X, Z, Y.
def extent_of(obj):
    if getattr(obj, UNBOUNDED_PROPERTY):
        return [0.0, 0.0, 0.0]
    if obj.type == "EMPTY":
        size = obj.empty_display_size
        return [size, size, size]
    xs, ys, zs = zip(*obj.bound_box)
    return [(max(xs) - min(xs)) / 2, (max(zs) - min(zs)) / 2, (max(ys) - min(ys)) / 2]


def environment_component(obj, warnings, folder):
    if not has_environment(obj) or not components.is_loaded(ENVIRONMENT_LIGHT):
        return None
    relative = folder.include(getattr(obj, MAP_PROPERTY), MAP_FOLDER) if getattr(obj, MAP_PROPERTY) else None
    if relative is None:
        warnings.append(f"{obj.name}: its environment light has no .hdr map, so it is not exported")
        return None
    fields = components.export_fields(obj, components.type_entry_of(ENVIRONMENT_LIGHT))
    fields["Map"] = relative
    fields["Extent"] = extent_of(obj)
    return ENVIRONMENT_LIGHT, fields


def on_component_changed(obj):
    if has_environment(obj) and obj.type == "EMPTY" and obj.empty_display_type != "CUBE":
        obj.empty_display_type = "CUBE"
    for scene in obj.users_scene:
        sync_world(scene)


# The unbounded light Rin would use: visible, with a map, highest Priority first.
def sky_of(scene):
    candidates = [obj for obj in scene.objects
                  if has_environment(obj) and getattr(obj, UNBOUNDED_PROPERTY) and getattr(obj, MAP_PROPERTY)
                  and not obj.hide_get() and components.is_loaded(ENVIRONMENT_LIGHT)]
    if not candidates:
        return None
    return max(candidates, key=lambda obj: (components.field_value(obj, ENVIRONMENT_LIGHT, "Priority"), obj.name))


def preview_world(create):
    for world in bpy.data.worlds:
        if world.get(WORLD_KEY):
            return world
    if not create:
        return None
    world = bpy.data.worlds.new(WORLD_NAME)
    world[WORLD_KEY] = True
    world.use_nodes = True
    nodes, links = world.node_tree.nodes, world.node_tree.links
    nodes.clear()
    coordinates = nodes.new("ShaderNodeTexCoord")
    mapping = nodes.new("ShaderNodeMapping")
    texture = nodes.new("ShaderNodeTexEnvironment")
    background = nodes.new("ShaderNodeBackground")
    output = nodes.new("ShaderNodeOutputWorld")
    for x, node in enumerate((coordinates, mapping, texture, background, output)):
        node.location = (x * 260, 0)
    mapping.inputs["Rotation"].default_value = PANORAMA_TURN
    links.new(coordinates.outputs["Generated"], mapping.inputs["Vector"])
    links.new(mapping.outputs["Vector"], texture.inputs["Vector"])
    links.new(texture.outputs["Color"], background.inputs["Color"])
    links.new(background.outputs["Background"], output.inputs["Surface"])
    return world


def node_of(world, kind):
    return next(node for node in world.node_tree.nodes if node.bl_idname == kind)


# Values are only written when they change, since every write queues another depsgraph update.
def show_sky(scene, sky):
    world = preview_world(create=True)
    texture = node_of(world, "ShaderNodeTexEnvironment")
    image = bpy.data.images.load(bpy.path.abspath(getattr(sky, MAP_PROPERTY)), check_existing=True)
    if texture.image != image:
        texture.image = image
    # Rin lights a map on the same photometric scale as its lumens; Blender's Strength is radiometric, like watts.
    strength = node_of(world, "ShaderNodeBackground").inputs["Strength"]
    value = components.field_value(sky, ENVIRONMENT_LIGHT, "Intensity") / previews.LUMENS_PER_WATT
    if abs(strength.default_value - value) > 1e-9:
        strength.default_value = value
    if scene.world != world:
        setattr(scene, PREVIOUS_WORLD_PROPERTY, scene.world)
        scene.world = world


def restore_world(scene):
    world = preview_world(create=False)
    if world is not None and scene.world == world:
        scene.world = getattr(scene, PREVIOUS_WORLD_PROPERTY)
        setattr(scene, PREVIOUS_WORLD_PROPERTY, None)


# While an unbounded environment light exists, the scene shows it through the add-on's own World and gets its own
# World back afterwards. Box lights have no Blender equivalent: EEVEE probes capture the scene, not a given image.
def sync_world(scene):
    sky = sky_of(scene)
    try:
        if sky is not None:
            show_sky(scene, sky)
        else:
            restore_world(scene)
    except RuntimeError:
        restore_world(scene)


def on_preview_input_changed(obj, _context):
    for scene in obj.users_scene:
        sync_world(scene)


@persistent
def on_depsgraph_update(scene, _depsgraph):
    sync_world(scene)


class RIN_OT_pick_environment_map(bpy.types.Operator, ImportHelper):
    """Pick the Radiance (.hdr) image this environment light uses"""

    bl_idname = "object.rin_pick_environment_map"
    bl_label = "Pick Environment Map"
    filename_ext = ".hdr"
    filter_glob: StringProperty(default="*.hdr", options={"HIDDEN"})

    def execute(self, context):
        setattr(context.object, MAP_PROPERTY, self.filepath)
        return {"FINISHED"}


class RIN_OT_remove_environment_light(bpy.types.Operator):
    """Remove the environment light"""

    bl_idname = "object.rin_remove_environment_light"
    bl_label = "Remove Environment Light"
    bl_options = {"REGISTER", "UNDO"}

    def invoke(self, context, event):
        return context.window_manager.invoke_confirm(
            self, event, title="Remove Environment Light?", message="Its fields are reset if you add it again.",
            confirm_text="Remove", icon="WARNING")

    def execute(self, context):
        return {"FINISHED"} if components.remove_component(context.object, ENVIRONMENT_LIGHT) else {"CANCELLED"}


class EnvironmentLightPanel:
    bl_label = "Environment Light"

    @classmethod
    def poll(cls, context):
        obj = context.object
        return obj is not None and components.is_loaded(ENVIRONMENT_LIGHT) and has_environment(obj)

    def draw_header_preset(self, context):
        self.layout.operator(RIN_OT_remove_environment_light.bl_idname, text="", icon="X", emboss=False)

    def draw(self, context):
        obj = context.object
        row = self.layout.row(align=True)
        row.prop(obj, MAP_PROPERTY)
        row.operator(RIN_OT_pick_environment_map.bl_idname, text="", icon="FILEBROWSER")
        self.layout.prop(obj, UNBOUNDED_PROPERTY)
        components.draw_fields(self.layout, obj, ENVIRONMENT_LIGHT, skip={"Extent"})
        hint = "Lights everything, like a sky; previewed as the scene's World" if getattr(obj, UNBOUNDED_PROPERTY) else "Takes over inside the box; scale the empty to size it"
        self.layout.label(text=hint, icon="INFO")


class RIN_PT_environment_light(EnvironmentLightPanel, bpy.types.Panel):
    bl_space_type = "PROPERTIES"
    bl_region_type = "WINDOW"
    bl_context = "object"


class RIN_PT_environment_light_sidebar(EnvironmentLightPanel, bpy.types.Panel):
    bl_space_type = "VIEW_3D"
    bl_region_type = "UI"
    bl_category = "Rin"


classes = (RIN_OT_pick_environment_map, RIN_OT_remove_environment_light, RIN_PT_environment_light,
           RIN_PT_environment_light_sidebar)


def register():
    for cls in classes:
        bpy.utils.register_class(cls)
    setattr(bpy.types.Object, MAP_PROPERTY, StringProperty(
        name="Map", description="A Radiance (.hdr) panorama", update=on_preview_input_changed))
    setattr(bpy.types.Object, UNBOUNDED_PROPERTY, BoolProperty(
        name="Unbounded", description="Light everything, like a sky, instead of only inside the box",
        update=on_preview_input_changed))
    setattr(bpy.types.Scene, PREVIOUS_WORLD_PROPERTY, PointerProperty(type=bpy.types.World))
    bpy.app.handlers.depsgraph_update_post.append(on_depsgraph_update)
    components.export_sources.append(environment_component)
    components.custom_panels.add(ENVIRONMENT_LIGHT)
    components.change_listeners.append(on_component_changed)


def unregister():
    bpy.app.handlers.depsgraph_update_post.remove(on_depsgraph_update)
    for scene in bpy.data.scenes:
        restore_world(scene)
    delattr(bpy.types.Scene, PREVIOUS_WORLD_PROPERTY)
    components.change_listeners.remove(on_component_changed)
    components.custom_panels.discard(ENVIRONMENT_LIGHT)
    components.export_sources.remove(environment_component)
    delattr(bpy.types.Object, UNBOUNDED_PROPERTY)
    delattr(bpy.types.Object, MAP_PROPERTY)
    for cls in reversed(classes):
        bpy.utils.unregister_class(cls)
