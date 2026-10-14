import shutil

from silo import Component, Package, SiloPackage


class VulkanMemoryAllocator(SiloPackage):
    name = "vulkan-memory-allocator"
    version = "3.1.0"
    header_only = True

    def source(self, src):
        src.git(
            "https://github.com/GPUOpen-LibrariesAndSDKs/VulkanMemoryAllocator.git",
            commit="009ecd192c1289c7529bff248a16cfe896254816",
        )

    def build(self):
        # One header. Whoever defines VMA_IMPLEMENTATION compiles it.
        shutil.copytree(self.source_dir / "include", self.install_dir / "include")

    def package(self):
        headers = Component("vulkan-memory-allocator", headers=["include"])
        return Package(components=[headers], licenses=["LICENSE.txt"])
