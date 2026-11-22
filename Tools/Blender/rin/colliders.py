import bmesh
import bpy
from bpy.app.handlers import persistent
from bpy.props import EnumProperty, FloatProperty, PointerProperty
from mathutils import Matrix

from . import components

RIGID_BODY = "Rin.World.Physics.RigidBody"
COLLIDER_KEY = "rin_collider"
GROUP_KEY = "rin_colliders"

# The glTF exporter skips properties whose names start with "gltf".
COLLIDER_PROPERTY = "gltf_rin_collider"

CAPSULE_SEGMENTS = 16


def rebuild_capsule(settings, _context):
    obj = settings.id_data
    if obj.type == "MESH":
        build_capsule_mesh(obj.data, settings.radius, settings.half_height)


class RIN_PG_collider(bpy.types.PropertyGroup):
    kind: EnumProperty(items=[("NONE", "None", ""), ("BOX", "Box", ""), ("SPHERE", "Sphere", ""),
                              ("CAPSULE", "Capsule", "")], default="NONE")
    radius: FloatProperty(name="Radius", default=0.5, min=0.001, update=rebuild_capsule)
    half_height: FloatProperty(name="Half Height", default=0.5, min=0.0, update=rebuild_capsule,
                               description="Half the length between the centres of the two caps")


def settings_of(obj):
    return getattr(obj, COLLIDER_PROPERTY)


def is_collider(obj):
    return settings_of(obj).kind != "NONE"


def is_group(obj):
    return bool(obj.get(GROUP_KEY))


def has_body(obj):
    return components.components_of(obj).has(RIGID_BODY)


def colliders_of(owner):
    group = group_of(owner, create=False)
    return [] if group is None else [child for child in group.children_recursive if is_collider(child)]


def delete_colliders(owner):
    group = group_of(owner, create=False)
    if group is None:
        return
    for obj in [group, *group.children_recursive]:
        bpy.data.objects.remove(obj)


def group_of(owner, create):
    for child in owner.children:
        if is_group(child):
            return child
    if not create:
        return None
    group = bpy.data.objects.new(group_name(owner), None)
    group[GROUP_KEY] = True
    group.empty_display_size = 0.1
    group.parent = owner
    for collection in owner.users_collection:
        collection.objects.link(group)
    return group


def group_name(owner):
    return f"{owner.name} Colliders"


# Blender adds ".001" when the name is taken, which still counts as following the owner, so the handler never fights
# over a name it cannot have.
def follow_owner_name(group):
    if group.parent is None:
        return
    name = group_name(group.parent)
    if group.name != name and not group.name.startswith(name + "."):
        group.name = name


# Keeps each Colliders empty named after its owner, through renames of either.
@persistent
def on_depsgraph_update(scene, depsgraph):
    for update in depsgraph.updates:
        if not isinstance(update.id, bpy.types.Object):
            continue
        obj = scene.objects.get(update.id.name)
        if obj is None:
            continue
        if is_group(obj):
            follow_owner_name(obj)
        elif (group := group_of(obj, create=False)) is not None:
            follow_owner_name(group)


def build_capsule_mesh(mesh, radius, half_height):
    shape = bmesh.new()
    bmesh.ops.create_uvsphere(shape, u_segments=CAPSULE_SEGMENTS, v_segments=CAPSULE_SEGMENTS // 2, radius=radius)
    for vertex in shape.verts:
        vertex.co.z += half_height if vertex.co.z > 1e-6 else -half_height if vertex.co.z < -1e-6 else 0.0
    shape.to_mesh(mesh)
    shape.free()


def local_bounds(obj):
    if obj.type != "MESH":
        return (0.0, 0.0, 0.0), (1.0, 1.0, 1.0)
    xs, ys, zs = zip(*obj.bound_box)
    centre = ((min(xs) + max(xs)) / 2, (min(ys) + max(ys)) / 2, (min(zs) + max(zs)) / 2)
    return centre, (max(xs) - min(xs), max(ys) - min(ys), max(zs) - min(zs))


def create_collider(owner, kind):
    group = group_of(owner, create=True)
    centre, size = local_bounds(owner)
    name = f"{owner.name} {kind.title()}"
    if kind == "CAPSULE":
        obj = bpy.data.objects.new(name, bpy.data.meshes.new(name))
        obj.display_type = "WIRE"
        obj.hide_render = True
    else:
        obj = bpy.data.objects.new(name, None)
        obj.empty_display_type = "CUBE" if kind == "BOX" else "SPHERE"
        obj.empty_display_size = 0.5
    settings_of(obj).kind = kind
    obj.parent = group
    obj.location = centre
    if kind == "BOX":
        obj.scale = tuple(max(s, 1e-3) for s in size)
    elif kind == "SPHERE":
        obj.scale = (max(size) / 1.0,) * 3
    else:
        settings_of(obj).radius = max(size[0], size[1]) / 2
        settings_of(obj).half_height = max(size[2] / 2 - settings_of(obj).radius, 0.0)
    for collection in group.users_collection:
        collection.objects.link(obj)
    return obj


# Sizes are in the collider's own unscaled frame, so its object scale reaches Rin through the node transform. A Blender
# capsule runs along local Z, which is Rin's Y.
def collider_json(obj):
    settings = settings_of(obj)
    if settings.kind == "BOX":
        edge = 2.0 * obj.empty_display_size
        return {"Kind": "Box", "Size": [edge, edge, edge]}
    if settings.kind == "SPHERE":
        return {"Kind": "Sphere", "Radius": obj.empty_display_size}
    return {"Kind": "Capsule", "Radius": settings.radius, "HalfHeight": settings.half_height}


def owner_of(obj):
    parent = obj.parent
    while parent is not None and (is_collider(parent) or is_group(parent)):
        parent = parent.parent
    return parent


def is_sheared(matrix):
    location, rotation, scale = matrix.decompose()
    rebuilt = Matrix.LocRotScale(location, rotation, scale)
    return any(abs(a - b) > 1e-4 for row_a, row_b in zip(matrix, rebuilt) for a, b in zip(row_a, row_b))


def write_extras(scene, warnings):
    written = []
    for obj in scene.objects:
        if not is_collider(obj):
            continue
        owner = owner_of(obj)
        if owner is None:
            warnings.append(f"{obj.name}: a collider needs a parent object")
            continue
        if not has_body(owner):
            warnings.append(f"{obj.name}: {owner.name} has no Rigid Body, so its colliders are ignored")
        if is_sheared(owner.matrix_world.inverted() @ obj.matrix_world):
            warnings.append(f"{obj.name}: non-uniform scale with rotation shears it, which physics shapes cannot do")
        obj[COLLIDER_KEY] = collider_json(obj)
        written.append(obj)
    return written


def clear_extras(objects):
    for obj in objects:
        del obj[COLLIDER_KEY]


class RIN_OT_add_collider(bpy.types.Operator):
    """Add a collider shape under the active object's Colliders empty, sized to its mesh"""

    bl_idname = "object.rin_add_collider"
    bl_label = "Add Rin Collider"
    bl_options = {"REGISTER", "UNDO"}
    kind: EnumProperty(items=[("BOX", "Box", ""), ("SPHERE", "Sphere", ""), ("CAPSULE", "Capsule", "")])

    @classmethod
    def poll(cls, context):
        return context.object is not None and has_body(context.object)

    def execute(self, context):
        owner = context.object
        collider = create_collider(owner, self.kind)
        if self.kind == "CAPSULE":
            build_capsule_mesh(collider.data, settings_of(collider).radius, settings_of(collider).half_height)
        owner.select_set(False)
        collider.select_set(True)
        context.view_layer.objects.active = collider
        return {"FINISHED"}


class RIN_OT_select_collider(bpy.types.Operator):
    """Select this collider to move or scale it"""

    bl_idname = "object.rin_select_collider"
    bl_label = "Select Rin Collider"
    bl_options = {"REGISTER", "UNDO"}
    name: bpy.props.StringProperty()

    def execute(self, context):
        collider = bpy.data.objects.get(self.name)
        if collider is None:
            return {"CANCELLED"}
        for obj in context.selected_objects:
            obj.select_set(False)
        collider.select_set(True)
        context.view_layer.objects.active = collider
        return {"FINISHED"}


class RIN_OT_delete_collider(bpy.types.Operator):
    """Delete this collider"""

    bl_idname = "object.rin_delete_collider"
    bl_label = "Delete Rin Collider"
    bl_options = {"REGISTER", "UNDO"}
    name: bpy.props.StringProperty()

    def execute(self, context):
        collider = bpy.data.objects.get(self.name)
        if collider is None or not is_collider(collider):
            return {"CANCELLED"}
        group = collider.parent if collider.parent is not None and is_group(collider.parent) else None
        bpy.data.objects.remove(collider)
        if group is not None and not group.children:
            bpy.data.objects.remove(group)
        return {"FINISHED"}


class RIN_MT_add_collider(bpy.types.Menu):
    bl_label = "Add Collider"

    def draw(self, context):
        for kind, label, icon in (("BOX", "Box", "MESH_CUBE"), ("SPHERE", "Sphere", "MESH_UVSPHERE"),
                                  ("CAPSULE", "Capsule", "MESH_CAPSULE")):
            self.layout.operator(RIN_OT_add_collider.bl_idname, text=label, icon=icon).kind = kind


def draw_collider_fields(layout, obj):
    settings = settings_of(obj)
    box = layout.box()
    box.label(text=f"Rin {settings.kind.title()} Collider", icon="PHYSICS")
    if settings.kind == "CAPSULE":
        box.prop(settings, "radius")
        box.prop(settings, "half_height")
    else:
        box.label(text="Size it with the object's scale")
    owner = owner_of(obj)
    if owner is not None and not has_body(owner):
        box.label(text=f"{owner.name} has no Rigid Body", icon="ERROR")


ICONS = {"BOX": "MESH_CUBE", "SPHERE": "MESH_UVSPHERE", "CAPSULE": "MESH_CAPSULE"}


def draw_colliders(layout, owner):
    header = layout.row()
    header.label(text="Colliders")
    header.menu(RIN_MT_add_collider.__name__, icon="ADD")
    colliders = colliders_of(owner)
    if not colliders:
        hint = "None: the shape comes from the mesh bounds" if owner.type == "MESH" else "Add one to give it a shape"
        layout.label(text=hint)
    for collider in colliders:
        row = layout.row(align=True)
        row.label(text=collider.name, icon=ICONS[settings_of(collider).kind])
        row.operator(RIN_OT_select_collider.bl_idname, text="", icon="RESTRICT_SELECT_OFF").name = collider.name
        row.operator(RIN_OT_delete_collider.bl_idname, text="", icon="X").name = collider.name


classes = (RIN_PG_collider, RIN_OT_add_collider, RIN_OT_select_collider, RIN_OT_delete_collider, RIN_MT_add_collider)


def register():
    for cls in classes:
        bpy.utils.register_class(cls)
    setattr(bpy.types.Object, COLLIDER_PROPERTY, PointerProperty(type=RIN_PG_collider))
    components.panel_drawers.append(draw_panel)
    bpy.app.handlers.depsgraph_update_post.append(on_depsgraph_update)


def unregister():
    bpy.app.handlers.depsgraph_update_post.remove(on_depsgraph_update)
    components.panel_drawers.remove(draw_panel)
    delattr(bpy.types.Object, COLLIDER_PROPERTY)
    for cls in reversed(classes):
        bpy.utils.unregister_class(cls)


def draw_panel(layout, obj):
    if is_collider(obj):
        draw_collider_fields(layout, obj)
        return True
    if is_group(obj):
        layout.label(text="Colliders of its parent's Rigid Body", icon="INFO")
        return True
    return False
