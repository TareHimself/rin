import bpy
from mathutils import Vector

from . import colliders, components

RIGID_BODY = colliders.RIGID_BODY


def local_bounds(obj):
    corners = [Vector(corner) for corner in obj.bound_box]
    low = Vector(min(c[axis] for c in corners) for axis in range(3))
    high = Vector(max(c[axis] for c in corners) for axis in range(3))
    return (low + high) / 2.0, high - low


# Blender's local X, Y, Z become Rin's X, Z, Y.
def bounds_shape(obj, warnings):
    if obj.type != "MESH":
        return None
    center, size = local_bounds(obj)
    if not colliders.colliders_of(obj) and center.length > 0.01 * max(size.length, 1.0):
        warnings.append(f"{obj.name}: the box is centred on the object's origin, not on its mesh; add a collider to place it")
    return {"Kind": "Box", "Size": [size.x, size.z, size.y]}


# The mesh bounds give a fallback box; colliders, when there are any, replace it on import.
def rigid_body_component(obj, warnings, _folder):
    if not colliders.has_body(obj) or not components.is_loaded(RIGID_BODY):
        return None
    fields = components.export_fields(obj, components.type_entry_of(RIGID_BODY))
    shape = bounds_shape(obj, warnings)
    if shape is None and not colliders.colliders_of(obj):
        warnings.append(f"{obj.name}: a Rigid Body without a mesh needs a collider, so it is not exported")
        return None
    if shape is not None:
        fields["Shape"] = shape
    return RIGID_BODY, fields


class RIN_OT_remove_rigid_body(bpy.types.Operator):
    """Remove the Rin rigid body and its colliders"""

    bl_idname = "object.rin_remove_rigid_body"
    bl_label = "Remove Rigid Body"
    bl_options = {"REGISTER", "UNDO"}

    def invoke(self, context, event):
        count = len(colliders.colliders_of(context.object))
        message = f"Its {count} collider(s) are deleted too." if count else "Its fields are reset if you add it again."
        return context.window_manager.invoke_confirm(
            self, event, title="Remove Rigid Body?", message=message, confirm_text="Remove", icon="WARNING")

    def execute(self, context):
        obj = context.object
        colliders.delete_colliders(obj)
        return {"FINISHED"} if components.remove_component(obj, RIGID_BODY) else {"CANCELLED"}


class RigidBodyPanel:
    bl_label = "Rigid Body"

    @classmethod
    def poll(cls, context):
        obj = context.object
        return obj is not None and components.is_loaded(RIGID_BODY) and colliders.has_body(obj)

    def draw_header_preset(self, context):
        self.layout.operator(RIN_OT_remove_rigid_body.bl_idname, text="", icon="X", emboss=False)

    def draw(self, context):
        components.draw_fields(self.layout, context.object, RIGID_BODY)
        self.layout.separator()
        colliders.draw_colliders(self.layout, context.object)


class RIN_PT_rigid_body(RigidBodyPanel, bpy.types.Panel):
    bl_space_type = "PROPERTIES"
    bl_region_type = "WINDOW"
    bl_context = "object"


class RIN_PT_rigid_body_sidebar(RigidBodyPanel, bpy.types.Panel):
    bl_space_type = "VIEW_3D"
    bl_region_type = "UI"
    bl_category = "Rin"


classes = (RIN_OT_remove_rigid_body, RIN_PT_rigid_body, RIN_PT_rigid_body_sidebar)


def register():
    for cls in classes:
        bpy.utils.register_class(cls)
    components.export_sources.append(rigid_body_component)
    components.custom_panels.add(RIGID_BODY)


def unregister():
    components.custom_panels.discard(RIGID_BODY)
    components.export_sources.remove(rigid_body_component)
    for cls in reversed(classes):
        bpy.utils.unregister_class(cls)
