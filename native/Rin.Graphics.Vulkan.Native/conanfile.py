from pathlib import Path
import sys

THIS_FILE = Path(__file__).resolve()
REPO_ROOT = THIS_FILE.parents[2]

if str(REPO_ROOT) not in sys.path:
    sys.path.insert(0, str(REPO_ROOT))
    
from native.base_recipe import BaseRecipe  # noqa: E402


class RinFrameworkGraphicsNative(BaseRecipe):
    name = "Rin.Graphics.Native"
    version = "1.0.0"
    requires = ["vk-bootstrap/1.3.296", "msdfgen/1.12"]

    def requirements(self):
        if self.settings.os == "Macos":
            self.requires("rwin/1.0.1",options={ "compat" : True})
        else:
            self.requires("rwin/1.0.1")
