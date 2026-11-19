"""Builds blender_level.glb through the rin_blender add-on.

Run from the repo root:
    blender-launcher -b --factory-startup --python Engine/Rin.GLTF.Tests/Assets/make_blender_level.py
"""
import math
import os
import sys

import bpy

repo = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", ".."))
sys.path.insert(0, os.path.join(repo, "Tools"))
import rin_blender  # noqa: E402

rin_blender.register()
bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene

bpy.ops.mesh.primitive_cube_add(location=(2.0, 0.0, 0.0))
cube = bpy.context.object
cube.name = "RightCube"
bpy.ops.object.duplicate(linked=True)
bpy.context.object.name = "RightCubeTwin"
bpy.context.object.location = (2.0, 3.0, 0.0)

lamp = bpy.data.lights.new("Lamp", "POINT")
lamp.rin_lumens = 800.0
scene.collection.objects.link(bpy.data.objects.new("Lamp", lamp))
bpy.data.objects["Lamp"].location = (0.0, 0.0, 3.0)

sun = bpy.data.lights.new("Sun", "SUN")
sun.rin_lux = 100000.0
scene.collection.objects.link(bpy.data.objects.new("Sun", sun))
bpy.data.objects["Sun"].rotation_euler = (math.radians(45.0), 0.0, 0.0)

for name, location in (("Another", (5.0, 5.0, 5.0)), ("Overview", (0.0, -10.0, 1.0))):
    camera = bpy.data.objects.new(name, bpy.data.cameras.new(name))
    camera.location = location
    camera.rotation_euler = (math.radians(90.0), 0.0, 0.0)
    scene.collection.objects.link(camera)
scene.camera = bpy.data.objects["Overview"]

bpy.ops.export_scene.rin_level(filepath=os.path.join(os.path.dirname(__file__), "blender_level.glb"))
