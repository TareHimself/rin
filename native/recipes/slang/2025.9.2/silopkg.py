import zipfile

from silo import Component, InvalidConfiguration, Package, SharedLib, SiloPackage
from silo.enums import Architecture, Platform
from silo.source import download

KEPT_FOLDERS = ("include/", "lib/", "bin/", "LICENSE")
RELEASES = "https://github.com/shader-slang/slang/releases/download"
# The release archive for each system, with its sha256
ARCHIVES = {
    (Platform.Windows, Architecture.X86_64): (
        "windows-x86_64",
        "1e52578e3b7bb545363df35c9f9d382345511389d3b9c6d3b5ee05eecc34a31d",
    ),
    (Platform.Windows, Architecture.ARM64): (
        "windows-aarch64",
        "921a0dcb7f14c1c97ad8790f59ee653d823c98b8dfe1fb1c44ca0944dffb4fbc",
    ),
    (Platform.Linux, Architecture.X86_64): (
        "linux-x86_64",
        "913d0aa96902b439172074aceb411ea685af0aa6e5c7474c28c3a5654881d82c",
    ),
    (Platform.Linux, Architecture.ARM64): (
        "linux-aarch64",
        "77ce1197b00cbea6a58850a0fdb5de7e6db428ba95ded2a41bd426463feac347",
    ),
    (Platform.Darwin, Architecture.X86_64): (
        "macos-x86_64",
        "4e37959372b34c572a9d695b76dfa3625aacd379afbe0b624811ce3739b26252",
    ),
    (Platform.Darwin, Architecture.ARM64): (
        "macos-aarch64",
        "ecbb75223cb30a41891f70508834eaf1db8fe2e4458bc7740b7f1322db9a1a1a",
    ),
}
# The folder, file pattern and linked library name of the shared libraries on each system
LIBRARIES = {
    Platform.Windows: ("bin", "*.dll", "slang.dll"),
    Platform.Linux: ("lib", "*.so", "libslang.so"),
    Platform.Darwin: ("lib", "*.dylib", "libslang.dylib"),
}


class Slang(SiloPackage):
    """Slang as upstream's prebuilt release. Nothing is compiled."""

    name = "slang"
    version = "2025.9.2"
    platforms = (Platform.Windows, Platform.Linux, Platform.Darwin)

    @property
    def system(self) -> tuple[Platform, Architecture]:
        return self.profile.settings.os, self.profile.settings.arch

    def source(self, src):
        # src.fetch cannot unpack a zip archive, so build() downloads it
        src.empty()

    def validate(self):
        if self.system not in ARCHIVES:
            raise InvalidConfiguration(f"slang {self.version} has no release archive listed here")

    def build(self):
        name, sha256 = ARCHIVES[self.system]
        archive = download(f"{RELEASES}/v{self.version}/slang-{self.version}-{name}.zip", sha256)
        with zipfile.ZipFile(archive) as release:
            kept = [file for file in release.namelist() if file.startswith(KEPT_FOLDERS)]
            release.extractall(self.install_dir, kept)

    def package(self):
        folder, pattern, linked = LIBRARIES[self.profile.settings.os]
        compiler = Component("slang", headers=["include"], shared_libs=["slang"])
        # Loaded by slang when it runs, never linked. They only have to ship with it.
        runtime = Component("runtime")
        for library in sorted((self.install_dir / folder).glob(pattern)):
            if library.name != linked:
                runtime.shared_libs.append(SharedLib(f"{folder}/{library.name}"))
        return Package(components=[compiler, runtime])
