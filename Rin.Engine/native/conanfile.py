from conan import ConanFile
from conan.tools.cmake import CMakeToolchain, CMake, cmake_layout, CMakeDeps


class RinEngineNative(ConanFile):
    name = "rin-engine-native"
    version = "1.0.0"
    url = "https://github.com/TareHimself/rin.git"
    license = "MIT"
    requires = [
        "webmdx/1.0.0",
        "harfbuzz/11.2.1",
        "vk-bootstrap/1.3.296"
    ]
    git_tag = "main"
    settings = "os", "compiler", "build_type", "arch"
    exports_sources = "CMakeLists.txt", "lib/*", "include/*"

    
    def config_options(self):
        pass

    def layout(self):
        cmake_layout(self)

    def generate(self):
        deps = CMakeDeps(self)
        deps.generate()
        tc = CMakeToolchain(self)
        tc.user_presets_path = None
        #tc.variables["CMAKE_CXX_STANDARD"] = "20"
        tc.generate()

    def build(self):
        cmake = CMake(self)
        cmake.configure()
        cmake.build()

    def package(self):
        cmake = CMake(self)
        cmake.install()

    def package_info(self):
        pass
