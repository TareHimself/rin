from silo import Component, Package, SiloPackage
from silo.tools.cmake import CMakeBuild, CMakeDeps, CMakeGenerator, CMakePresets


class RinAudioMiniaudioNative(SiloPackage):
    name = "Rin.Audio.Miniaudio.Native"
    version = "1.0.0"

    def source(self, src):
        src.local(".")

    def deps(self):
        self.dep("miniaudio@0.11.22")
        self.tool("cmake@4.3")

    def generate(self):
        CMakeDeps(self).generate()
        toolchain = CMakeGenerator(self)
        toolchain.generate()

        # After `silo sync`: cmake --preset Rin-Debug && cmake --build --preset Rin-Debug
        configuration = "Debug" if self.is_debug else "Release"
        presets = CMakePresets(self, toolchain)
        presets.add(f"Rin-{configuration}", build_dir=f"build-{configuration.lower()}")
        presets.generate()

    def build(self):
        cmake = CMakeBuild(self)
        cmake.configure()
        cmake.build()
        cmake.install()

    def package(self):
        # bin holds the module and every shared library it loads, which is what gets packed
        return Package(components=[Component(self.name, content=["bin"])])
