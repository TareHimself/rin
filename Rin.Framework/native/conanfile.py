from conan import ConanFile
from conan.tools.cmake import CMakeToolchain, CMake, cmake_layout, CMakeDeps
import json
import os


class RinEngineNative(ConanFile):
    name = "rin-engine-native"
    version = "1.0.0"
    url = "https://github.com/TareHimself/rin.git"
    license = "MIT"
    requires = ["webmdx/1.0.0", "rwin/1.0.1", "harfbuzz/11.2.1", "vk-bootstrap/1.3.296"]
    git_tag = "main"
    settings = "os", "compiler", "build_type", "arch"
    exports_sources = "CMakeLists.txt", "lib/*", "include/*"
    is_release = False

    def config_options(self):
        self.is_release = self.settings.build_type == "Release"

    def layout(self):
        # cmake_layout(self)

        # if it does not happen in this order we get lowercase name
        if self.is_release:
            cmake_layout(self, build_folder="build/Release")
        else:
            cmake_layout(self, build_folder="build/Debug")

        self.folders.build_folder_vars = ["settings.build_type"]

    def generate(self):
        deps = CMakeDeps(self)
        deps.generate()
        tc = CMakeToolchain(self)
        if self.is_release:
            tc.cache_variables["CMAKE_BUILD_TYPE"] = "Release"
        else:
            tc.cache_variables["RIN_BUILD_TEST_MAIN"] = "ON"
            tc.cache_variables["CMAKE_BUILD_TYPE"] = "Debug"
        tc.generate()

        # Add custom presets to the generated presets file
        configuration = "Release" if self.is_release else "Debug"
        with open(os.path.join(os.getcwd(), "CMakePresets.json"), "r+") as f:
            jsonData = json.load(f)
            jsonData["configurePresets"][0]['name'] = f"Rin-{configuration}"
            jsonData["configurePresets"][0]['displayName'] = f"'Rin-{configuration}' config"
            jsonData["configurePresets"][0]['description'] = f"Configure made by conan modified by Rin"
            jsonData["configurePresets"] = [jsonData["configurePresets"][0]]

            jsonData["buildPresets"][0]['name'] = f"Rin-{configuration}"
            jsonData["buildPresets"][0]['configurePreset'] = f"Rin-{configuration}"
            jsonData["buildPresets"] = [jsonData["buildPresets"][0]]

            jsonData["testPresets"][0]['name'] = f"Rin-{configuration}"
            jsonData["testPresets"][0]['configurePreset'] = f"Rin-{configuration}"
            jsonData["testPresets"] = [jsonData["testPresets"][0]]

            f.seek(0)
            json.dump(jsonData, f, indent=4)
            f.truncate()

    def build(self):
        cmake = CMake(self)
        cmake.configure()
        cmake.build()

    def package(self):
        cmake = CMake(self)
        cmake.install()

    def package_info(self):
        pass
