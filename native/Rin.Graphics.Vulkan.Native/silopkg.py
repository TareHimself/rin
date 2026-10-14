from silo import Component, Package, SiloPackage
from silo.tools.cmake import CMakeBuild, CMakeDeps, CMakeGenerator, CMakePresets


class RinGraphicsVulkanNative(SiloPackage):
    name = "Rin.Graphics.Vulkan.Native"
    version = "1.0.0"

    def source(self, src):
        src.local(".")

    def deps(self):
        self.dep("rwin@1.0.0")
        self.dep("vk-bootstrap@1.3.296")
        self.dep("vulkan-headers@1.3.296")
        self.dep("vulkan-loader@1.3.296")
        self.dep("vulkan-memory-allocator@3.1.0")
        self.tool("cmake@4.3")

    def generate(self):
        deps = CMakeDeps(self)
        deps.alias("Vulkan::Vulkan", "vulkan-loader::vulkan-loader")
        deps.generate()
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
