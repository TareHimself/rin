from pathlib import Path
import sys

THIS_FILE = Path(__file__).resolve()
REPO_ROOT = THIS_FILE.parents[2]

if str(REPO_ROOT) not in sys.path:
    sys.path.insert(0, str(REPO_ROOT))
    
from native.base_recipe import BaseRecipe  # noqa: E402


class RinAudioMiniaudio(BaseRecipe):
    name = "Rin.Audio.Miniaudio"
    version = "1.0.0"
    requires = ["miniaudio/0.11.22"]
