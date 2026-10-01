"""Standalone check: identical CP-AnemiC images listed under different patient records.

Usage: python model/audit_duplicates.py [path/to/cp-anemic]
"""
import hashlib
import sys
from collections import defaultdict
from pathlib import Path

import numpy as np
import openpyxl
from PIL import Image

root = Path(sys.argv[1]) if len(sys.argv) > 1 else Path(__file__).resolve().parent.parent / "data" / "raw" / "cp-anemic"
sheet = openpyxl.load_workbook(root / "Anemia_Data_Collection_Sheet.xlsx", read_only=True).worksheets[0]
rows = list(sheet.iter_rows(values_only=True))
meta = {str(r[0]).strip(): dict(zip(rows[0], r)) for r in rows[1:] if r[0]}

groups = defaultdict(list)
for png in sorted(root.glob("*/*.png")):
    pixels = np.asarray(Image.open(png).convert("RGBA"))
    groups[hashlib.md5(pixels.tobytes()).hexdigest()].append(png.stem)

dups = sorted((g for g in groups.values() if len(g) > 1), key=len, reverse=True)
total = sum(len(g) for g in groups.values())
print(f"{total} images, {len(dups)} groups of identical images covering {sum(map(len, dups))} images\n")
for g in dups:
    print(f"{len(g)} copies:")
    for image_id in g:
        m = meta.get(image_id, {})
        print(f"   {image_id}  Hb={m.get('HB_LEVEL')}  age={m.get('Age(Months)')}m  "
              f"{m.get('GENDER')}  {m.get('HOSPITAL')}  {m.get('REMARK')}")
