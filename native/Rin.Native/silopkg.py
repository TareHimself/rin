from silo import Component, Package, SiloPackage
from silo.tools.cmake import CMakeBuild, CMakeDeps, CMakeGenerator, CMakePresets


class RinNative(SiloPackage):
    name = "Rin.Native"
    version = "1.0.0"

    def source(self, src):
        src.local(".")

    def deps(self):
        self.dep("webmdx@1.0.0", options={"shared": False})
        # Only the core library is used: shapes in, a distance field out
        core_only = {"with_png": False, "with_svg": False, "standalone": False}
        self.dep("msdfgen@1.12", options=core_only)
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
