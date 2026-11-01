from __future__ import annotations

import hashlib
import json
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent
SOURCE = ROOT.parent / "patched_font_4564F3D7.str"
OUTPUT = ROOT / "patched_font_4564F3D7.workbook.str"
RESEARCH = Path(
    r"E:\DS2-Severed-Patch\upstream\DS2SeveredPC-20261031\release-source\tools\research"
)
sys.path.insert(0, str(RESEARCH))
from ds2 import streamfile, streamwrite  # noqa: E402


def sha256(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


source_data = SOURCE.read_bytes()
be, assets = streamfile.read(source_data)
if be:
    raise ValueError("expected a PC stream")

replacements = {
    ("tg4d", "eurostileltstdbold32"): ROOT
    / "ds2output/tg4d/0005_eurostileltstdbold32.tg4d",
    ("tg4d", "ishimura_sans_bold"): ROOT
    / "ds2output/tg4d/0007_ishimura_sans_bold.tg4d",
    ("tg4d", "russellsquare32"): ROOT
    / "ds2output/tg4d/0009_russellsquare32.tg4d",
    ("FFN", "russellsquare32.ffn"): ROOT
    / "ds2output/FFN/0512_russellsquare32.inf",
    ("FFN", "eurostileltstdbold32.ffn"): ROOT
    / "ds2output/FFN/0513_eurostileltstdbold32.inf",
    ("FFN", "ishimura_sans_bold.ffn"): ROOT
    / "ds2output/FFN/0514_ishimura_sans_bold.inf",
}

writer = streamwrite.Writer()
replaced = []
for asset in assets:
    key = (asset.type, asset.name)
    payload = replacements[key].read_bytes() if key in replacements else asset.payload()
    if key in replacements:
        if len(payload) != asset.usize:
            raise ValueError(
                f"{key}: replacement size {len(payload)} != original {asset.usize}"
            )
        replaced.append(
            {
                "type": asset.type,
                "name": asset.name,
                "path": asset.path,
                "bytes": len(payload),
                "sha256": sha256(payload),
            }
        )
    writer.add_asset(asset, payload)

output = writer.data()
_, readback = streamfile.read(output)
if len(readback) != len(assets):
    raise ValueError("asset count changed")
for original, actual in zip(assets, readback):
    key = (original.type, original.name)
    expected = replacements[key].read_bytes() if key in replacements else original.payload()
    if actual.payload() != expected:
        raise ValueError(f"readback mismatch: {key}")

OUTPUT.write_bytes(output)
report = {
    "source": str(SOURCE),
    "source_sha256": sha256(source_data),
    "output": str(OUTPUT),
    "output_bytes": len(output),
    "output_sha256": sha256(output),
    "asset_count": len(assets),
    "replaced": replaced,
    "readback": "all assets exact",
}
(ROOT / "font-stream-report.json").write_text(
    json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8"
)
print(json.dumps(report, ensure_ascii=False, indent=2))
