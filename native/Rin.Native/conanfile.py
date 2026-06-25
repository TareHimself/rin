from pathlib import Path
import sys

THIS_FILE = Path(__file__).resolve()
REPO_ROOT = THIS_FILE.parents[2]

if str(REPO_ROOT) not in sys.path:
    sys.path.insert(0, str(REPO_ROOT))
    
from native.base_recipe import BaseRecipe  # noqa: E402


class RinNative(BaseRecipe):
    name = "Rin.Native"
    version = "1.0.0"
    requires = ["webmdx/1.0.0", "harfbuzz/11.4.1", "msdfgen/1.12"]
