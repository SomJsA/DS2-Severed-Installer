from pathlib import Path
import importlib.util
import os
import sys

ROOT = Path(__file__).resolve().parent / "font_build"
os.chdir(ROOT)
sys.path.insert(0, str(ROOT))
SPEC = importlib.util.spec_from_file_location("font_generator", ROOT / "generator_v1.1.py")
GENERATOR = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(GENERATOR)

SWEI = "font"
TITLE = "mstiffhei"
GENERATOR.ds2["0512_russellsquare32.inf"]["font4chinese"] = SWEI
GENERATOR.ds2["0512_russellsquare32.inf"]["font4origin"] = SWEI
GENERATOR.ds2["0513_eurostileltstdbold32.inf"]["font4chinese"] = TITLE
GENERATOR.ds2["0513_eurostileltstdbold32.inf"]["font4origin"] = TITLE
GENERATOR.ds2["0514_ishimura_sans_bold.inf"]["font4chinese"] = TITLE
GENERATOR.ds2["0514_ishimura_sans_bold.inf"]["font4origin"] = TITLE

for name, info in GENERATOR.ds2.items():
    GENERATOR.generate(name, info)

print("SWAPPED_FONT_ATLASES_GENERATED")
