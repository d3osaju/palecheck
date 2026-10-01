"""Second-pass audit: near-identical images (re-cropped, re-encoded or resized copies) in CP-AnemiC.

Candidate pairs come from a coarse 64-bit difference hash; each candidate is then compared pixel by pixel
after flattening onto white and resizing both to a common 128x64 grid. A pair is "near-identical" when
the aspect ratios agree within 5% and the mean absolute difference is at most MAD_MAX levels (0-255).

Usage: python model/near_duplicates.py      (prints pairs; train.py imports find_near_pairs)
"""
from pathlib import Path

import numpy as np
from PIL import Image

MAD_MAX = 6.0
HASH_BITS_MAX = 6


def _flat(rgba):
    im = Image.fromarray(rgba)
    bg = Image.new("RGBA", im.size, (255, 255, 255, 255))
    bg.alpha_composite(im)
    return bg.convert("RGB")


def _dhash(flat):
    s = np.asarray(flat.resize((9, 8), Image.BILINEAR), dtype=int).sum(axis=2)
    return (s[:, :8] > s[:, 1:]).flatten()


def _grid(flat):
    return np.asarray(flat.resize((128, 64), Image.BILINEAR), dtype=float)


def find_near_pairs(items):
    """items: dicts with 'id' and 'rgba'. Returns [(id_a, id_b, mad)] for near-identical, non-identical pairs."""
    feats = []
    for it in items:
        flat = _flat(it["rgba"])
        h, w = it["rgba"].shape[:2]
        feats.append((it["id"], _dhash(flat), _grid(flat), w / h, it["rgba"].tobytes()))
    pairs = []
    for i in range(len(feats)):
        for j in range(i + 1, len(feats)):
            a, b = feats[i], feats[j]
            if a[4] == b[4]:
                continue  # exact copies are handled by the exact audit
            if (a[1] != b[1]).sum() > HASH_BITS_MAX or abs(a[3] / b[3] - 1) > 0.05:
                continue
            mad = float(np.abs(a[2] - b[2]).mean())
            if mad <= MAD_MAX:
                pairs.append((a[0], b[0], round(mad, 2)))
    return pairs


if __name__ == "__main__":
    root = Path(__file__).resolve().parent.parent / "data" / "raw" / "cp-anemic"
    items = [{"id": p.stem, "rgba": np.asarray(Image.open(p).convert("RGBA"))} for p in sorted(root.glob("*/*.png"))]
    pairs = find_near_pairs(items)
    print(f"{len(pairs)} near-identical pairs")
    for p in pairs:
        print(p)
