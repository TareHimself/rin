"""Builds the blender_level folder (level.glb and its environment map) through the Rin Blender add-on.

Run from the repo root:
    blender-launcher -b --factory-startup --python Engine/Rin.GLTF.Tests/Assets/make_blender_level.py
"""
import math
import os
import sys

import bpy

repo = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", ".."))
sys.path.insert(0, os.path.join(repo, "Tools", "Blender"))
import rin  # noqa: E402

rin.register()
bpy.ops.wm.read_factory_settings(use_empty=True)
rin.components.load_schema(os.path.join(os.path.dirname(__file__), "editor.schema.json"))
scene = bpy.context.scene

bpy.ops.mesh.primitive_cube_add(location=(2.0, 0.0, 0.0))
cube = bpy.context.object
cube.name = "RightCube"
bpy.ops.object.duplicate(linked=True)
bpy.context.object.name = "RightCubeTwin"
bpy.context.object.location = (2.0, 3.0, 0.0)



def add_rigid_body(obj, state="Static", **fields):
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.rin_add_component(type_name="Rin.World.Physics.RigidBody")
    body = rin.components.components_of(obj).fields_of("Rin.World.Physics.RigidBody")
    body.State = state
    for name, value in fields.items():
        setattr(body, name, value)


def add_collider(owner, kind):
    bpy.context.view_layer.objects.active = owner
    bpy.ops.object.rin_add_collider(kind=kind)
    return bpy.context.view_layer.objects.active


twin = bpy.data.objects["RightCubeTwin"]
add_rigid_body(cube, "Simulated", Mass=2.0)
add_rigid_body(twin, "Controlled", IsTrigger=True, CollisionChannel=3)
add_collider(twin, "SPHERE")

bpy.ops.mesh.primitive_cylinder_add(radius=0.5, depth=3.0, location=(-2.0, 0.0, 1.5))
tower = bpy.context.object
tower.name = "Tower"
add_rigid_body(tower)
add_collider(tower, "CAPSULE")



def add_box_collider(owner, location, scale):
    collider = add_collider(owner, "BOX")
    collider.location = location
    collider.scale = scale


bpy.ops.mesh.primitive_cube_add(location=(0.0, 4.0, 0.0))
workbench = bpy.context.object
workbench.name = "Workbench"
add_rigid_body(workbench)
add_box_collider(workbench, (0.0, 0.0, 0.9), (1.2, 0.8, 0.1))
for x in (-0.5, 0.5):
    for y in (-0.3, 0.3):
        add_box_collider(workbench, (x, y, 0.45), (0.1, 0.1, 0.9))

bpy.ops.mesh.primitive_cube_add(location=(4.0, 4.0, 0.0))
wardrobe = bpy.context.object
wardrobe.name = "Wardrobe"
add_rigid_body(wardrobe)
add_box_collider(wardrobe, (0.0, 0.0, 0.0), (1.0, 2.0, 0.5))

ENVIRONMENT_LIGHT = "Rin.World.Graphics.ImageBasedLighting.EnvironmentLight"
SKY_HDR = os.path.join(os.path.dirname(__file__), "sky.hdr")


def add_environment(name, location, unbounded):
    empty = bpy.data.objects.new(name, None)
    empty.location = location
    scene.collection.objects.link(empty)
    bpy.context.view_layer.objects.active = empty
    bpy.ops.object.rin_add_component(type_name=ENVIRONMENT_LIGHT)
    empty.gltf_rin_environment_map = SKY_HDR
    empty.gltf_rin_environment_unbounded = unbounded
    empty.empty_display_size = 3.0
    return rin.components.components_of(empty).fields_of(ENVIRONMENT_LIGHT)


add_environment("Sky", (0.0, 0.0, 20.0), unbounded=True).Intensity = 1.2
add_environment("Room", (0.0, 4.0, 1.0), unbounded=False).BlendDistance = 0.5

bpy.context.view_layer.objects.active = cube
bpy.ops.object.rin_add_component(type_name="Rin.GLTF.Tests.TestDoor")
door = rin.components.components_of(cube).fields_of("Rin.GLTF.Tests.TestDoor")
door.OpenSeconds = 2.5
door.IsLocked = True
door.Key = bpy.data.objects["RightCubeTwin"]

POINT_LIGHT = "Rin.World.Graphics.PointLight"
DIRECTIONAL_LIGHT = "Rin.World.Graphics.DirectionalLight"


def add_empty(name, location, rotation, component):
    empty = bpy.data.objects.new(name, None)
    empty.location = location
    empty.rotation_euler = rotation
    scene.collection.objects.link(empty)
    bpy.context.view_layer.objects.active = empty
    bpy.ops.object.rin_add_component(type_name=component)
    return rin.components.components_of(empty).fields_of(component)


add_empty("Lamp", (0.0, 0.0, 3.0), (0.0, 0.0, 0.0), POINT_LIGHT).Lumens = 800.0
# An empty's Rin forward is its -Y, so a quarter turn about X points the sun straight down.
add_empty("Sun", (0.0, 0.0, 10.0), (math.radians(90.0), 0.0, 0.0), DIRECTIONAL_LIGHT).Lux = 100000.0

for name, location in (("Another", (5.0, 5.0, 5.0)), ("Overview", (0.0, -10.0, 1.0))):
    camera = bpy.data.objects.new(name, bpy.data.cameras.new(name))
    camera.location = location
    camera.rotation_euler = (math.radians(90.0), 0.0, 0.0)
    scene.collection.objects.link(camera)
scene.camera = bpy.data.objects["Overview"]

bpy.ops.export_scene.rin_level(directory=os.path.join(os.path.dirname(__file__), "blender_level"))
