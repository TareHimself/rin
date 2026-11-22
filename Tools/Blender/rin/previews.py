import math

import bpy
from bpy.app.handlers import persistent

from . import components

POINT_LIGHT = "Rin.World.Graphics.PointLight"
DIRECTIONAL_LIGHT = "Rin.World.Graphics.DirectionalLight"
LIGHT_TYPES = {POINT_LIGHT: "POINT", DIRECTIONAL_LIGHT: "SUN"}

COLLECTION_NAME = "Rin Preview"
COLLECTION_KEY = "rin_preview"

# Blender lights are watts, and the glTF exporter and EEVEE both treat a watt as 683 lumens.
LUMENS_PER_WATT = 683.0

# Matches Rin's PointLight.CutoffLux, so the preview reaches as far as the light does in Rin.
CUTOFF_LUX = 0.01

# Rin's forward is Blender's -Y after export, and a Blender sun shines down its own -Z.
SUN_ROTATION = (-math.pi / 2.0, 0.0, 0.0)


def preview_collection(scene, create):
    for collection in scene.collection.children:
        if collection.get(COLLECTION_KEY):
            return collection
    if not create:
        return None
    collection = bpy.data.collections.new(COLLECTION_NAME)
    collection[COLLECTION_KEY] = True
    collection.hide_select = True
    scene.collection.children.link(collection)
    return collection


def previews_of(obj):
    return {child[components.PREVIEW_OF_KEY]: child for child in obj.children if components.is_preview(child)}


def wanted_light_types(obj):
    added = components.components_of(obj)
    return [name for name in LIGHT_TYPES if added.has(name) and components.is_loaded(name)]


def sync_object(obj, scene, update_existing=True):
    if components.is_preview(obj):
        return
    previews = previews_of(obj)
    for type_name in wanted_light_types(obj):
        preview = previews.pop(type_name, None)
        if preview is None:
            preview = create_preview(obj, scene, type_name)
        else:
            follow_owner_name(preview, obj, type_name)
            if not update_existing:
                continue
        apply_light(preview.data, obj, type_name)
    for preview in previews.values():
        remove_preview(preview)


def preview_name(owner, type_name):
    return f"{owner.name}.{type_name.rsplit('.', 1)[-1]} preview"


def follow_owner_name(preview, owner, type_name):
    name = preview_name(owner, type_name)
    if preview.name != name:
        preview.name = name
        preview.data.name = name


def create_preview(owner, scene, type_name):
    name = preview_name(owner, type_name)
    preview = bpy.data.objects.new(name, bpy.data.lights.new(name, LIGHT_TYPES[type_name]))
    preview[components.PREVIEW_OF_KEY] = type_name
    preview.parent = owner
    if type_name == DIRECTIONAL_LIGHT:
        preview.rotation_euler = SUN_ROTATION
    preview_collection(scene, create=True).objects.link(preview)
    return preview


def remove_preview(preview):
    light = preview.data
    bpy.data.objects.remove(preview)
    if light.users == 0:
        bpy.data.lights.remove(light)


def apply_light(light, owner, type_name):
    light.color = components.field_value(owner, type_name, "Color")[:3]
    if type_name == POINT_LIGHT:
        lumens = components.field_value(owner, type_name, "Lumens")
        radius = components.field_value(owner, type_name, "Radius")
        light.energy = lumens / LUMENS_PER_WATT
        # EEVEE culls a light by power alone, ignoring exposure, so dim lights would vanish without this.
        light.use_custom_distance = True
        # A radius of zero lets Rin derive it from the lumens, as PointLight does.
        light.cutoff_distance = radius if radius > 0.0 else math.sqrt(lumens / (4.0 * math.pi) / CUTOFF_LUX)
    else:
        light.energy = components.field_value(owner, type_name, "Lux") / LUMENS_PER_WATT


def remove_orphans(scene):
    collection = preview_collection(scene, create=False)
    if collection is None:
        return
    for preview in list(collection.objects):
        owner = preview.parent
        if owner is None or preview[components.PREVIEW_OF_KEY] not in wanted_light_types(owner):
            remove_preview(preview)


def sync_scene(scene):
    for obj in list(scene.objects):
        sync_object(obj, scene)
    remove_orphans(scene)


def on_component_changed(obj):
    for scene in obj.users_scene:
        sync_object(obj, scene)


# Catches duplicates, which copy an owner's components but not its unselectable preview, and renames. Existing
# previews are otherwise left alone, because writing to them would queue another depsgraph update.
@persistent
def on_depsgraph_update(scene, depsgraph):
    for update in depsgraph.updates:
        if isinstance(update.id, bpy.types.Object):
            obj = scene.objects.get(update.id.name)
            if obj is not None:
                sync_object(obj, scene, update_existing=False)
    remove_orphans(scene)


@persistent
def on_load(_file_path):
    for scene in bpy.data.scenes:
        sync_scene(scene)


class ExportExclusion:
    def __init__(self, context):
        self._layer = None
        collection = preview_collection(context.scene, create=False)
        if collection is not None:
            self._layer = context.view_layer.layer_collection.children.get(collection.name)

    def __enter__(self):
        if self._layer is not None:
            self._layer.exclude = True

    def __exit__(self, *_):
        if self._layer is not None:
            self._layer.exclude = False


def register():
    components.change_listeners.append(on_component_changed)
    bpy.app.handlers.depsgraph_update_post.append(on_depsgraph_update)
    bpy.app.handlers.load_post.append(on_load)


def unregister():
    bpy.app.handlers.load_post.remove(on_load)
    bpy.app.handlers.depsgraph_update_post.remove(on_depsgraph_update)
    components.change_listeners.remove(on_component_changed)
