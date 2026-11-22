import bpy
from bpy.app.handlers import persistent

from . import colliders, previews

# Each owner's visibility as last seen, so a change can be passed on while a collider hidden on its own stays hidden.
_last_seen = {}


def visibility_of(obj):
    return obj.hide_get(), obj.hide_viewport, obj.hide_render


def apply(obj, visibility):
    hidden, hidden_in_viewport, hidden_in_render = visibility
    if obj.hide_get() != hidden:
        obj.hide_set(hidden)
    if obj.hide_viewport != hidden_in_viewport:
        obj.hide_viewport = hidden_in_viewport
    if obj.hide_render != hidden_in_render:
        obj.hide_render = hidden_in_render


def collider_parts(owner):
    group = colliders.group_of(owner, create=False)
    return [] if group is None else [group, *group.children_recursive]


def owners(scene):
    found = set()
    collection = previews.preview_collection(scene, create=False)
    if collection is not None:
        found.update(preview.parent for preview in collection.objects if preview.parent is not None)
    found.update(obj.parent for obj in scene.objects if colliders.is_group(obj) and obj.parent is not None)
    return found


# Preview lights always match their owner. Colliders only follow a change, so hiding one on its own sticks.
def sync(scene, view_layer):
    for owner in owners(scene):
        if owner.name not in view_layer.objects:
            continue
        visibility = visibility_of(owner)
        for preview in previews.previews_of(owner).values():
            apply(preview, visibility)
        key = owner.as_pointer()
        if _last_seen.get(key, visibility) != visibility:
            for part in collider_parts(owner):
                apply(part, visibility)
        _last_seen[key] = visibility


@persistent
def on_depsgraph_update(scene, depsgraph):
    sync(scene, depsgraph.view_layer)


def register():
    bpy.app.handlers.depsgraph_update_post.append(on_depsgraph_update)


def unregister():
    bpy.app.handlers.depsgraph_update_post.remove(on_depsgraph_update)
    _last_seen.clear()
