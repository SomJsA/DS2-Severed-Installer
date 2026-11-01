from __future__ import annotations

import json
from pathlib import Path

HERE = Path(__file__).resolve().parent
WORKBOOK = HERE.parent / "workbook_translations.json"
CHARSET = HERE / "data" / "ds2texts.txt"

rows = json.loads(WORKBOOK.read_text(encoding="utf-8"))["rows"]
original = CHARSET.read_text(encoding="utf-8")
combined = original + "\n" + "\n".join(row["chinese"] for row in rows) + "\n"
unique = sorted(set(combined))

if len(unique) > 2348:
    raise ValueError(f"font atlas supports 2348 glyphs, requested {len(unique)}")

CHARSET.write_text(combined, encoding="utf-8")
print(
    json.dumps(
        {
            "charset": str(CHARSET),
            "original_unique": len(set(original)),
            "final_unique": len(unique),
            "remaining_capacity": 2348 - len(unique),
        },
        indent=2,
    )
)
