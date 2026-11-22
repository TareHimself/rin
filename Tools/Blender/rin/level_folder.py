import os
import shutil

import bpy

LEVEL_FILE = "level.glb"


class LevelFolder:
    """The folder a level is exported to: level.glb plus a copy of every file it names, so the folder moves as one."""

    def __init__(self, directory):
        self.directory = directory
        self._relative_by_source = {}

    @property
    def level_path(self):
        return os.path.join(self.directory, LEVEL_FILE)

    # Returns the copy's path relative to level.glb, or None when the file does not exist.
    def include(self, path, subfolder):
        source = os.path.normcase(os.path.abspath(bpy.path.abspath(path)))
        if not os.path.isfile(source):
            return None
        if source in self._relative_by_source:
            return self._relative_by_source[source]
        relative = self._free_name(subfolder, os.path.basename(source))
        destination = os.path.join(self.directory, *relative.split("/"))
        os.makedirs(os.path.dirname(destination), exist_ok=True)
        if not is_same_file(source, destination):
            shutil.copy2(source, destination)
        self._relative_by_source[source] = relative
        return relative

    # Two different files with the same name both get a place: the second becomes name_2, name_3, ...
    def _free_name(self, subfolder, name):
        stem, extension = os.path.splitext(name)
        taken = set(self._relative_by_source.values())
        candidate, number = f"{subfolder}/{name}", 2
        while candidate in taken:
            candidate, number = f"{subfolder}/{stem}_{number}{extension}", number + 1
        return candidate


def is_same_file(source, destination):
    if not os.path.isfile(destination):
        return False
    a, b = os.stat(source), os.stat(destination)
    return a.st_size == b.st_size and int(a.st_mtime) == int(b.st_mtime)
